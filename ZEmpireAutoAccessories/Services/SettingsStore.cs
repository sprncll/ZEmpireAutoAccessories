using System.Text.Json;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// The shop's settings, in a JSON file beside the cut sizes.
    ///
    /// Not a table, for the reason the cut sizes are not one: the database is
    /// the shop's own and nothing may be added to it. A file travels with the
    /// backup, reads in one line, and is a sensible home for a dozen values
    /// that change once a year.
    ///
    /// Read once at startup and pushed into the two places that answer from
    /// it - <see cref="Vat"/> and <see cref="StockLevels"/> - so the rest of
    /// the app asks the same question it always did and gets the shop's
    /// answer instead of a programmer's.
    ///
    /// A singleton, because the cache is the point; the lock makes it safe to
    /// share. Written to one side and moved into place, so a process that
    /// dies mid-write leaves the old file rather than half a new one.
    /// </summary>
    public class SettingsStore : ISettingsStore
    {
        private readonly string _path;
        private readonly ILogger<SettingsStore> _logger;
        private readonly object _gate = new();

        private ShopSettings _current = new();

        public SettingsStore(IConfiguration config, IWebHostEnvironment environment, ILogger<SettingsStore> logger)
        {
            _logger = logger;

            var configured = config["Shop:SettingsFile"];

            _path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "shop-settings.json")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);

            Publish(Read());
        }

        public ShopSettings Current
        {
            get { lock (_gate) { return _current; } }
        }

        public async Task Save(ShopSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            var temporary = _path + ".tmp";
            await File.WriteAllTextAsync(temporary, json);
            File.Move(temporary, _path, overwrite: true);

            Publish(settings);
        }

        /// <summary>
        /// Takes the settings as the cache and hands them to the two static
        /// holders the rest of the app reads. Assigned whole rather than
        /// mutated, so a reader never sees half an update.
        /// </summary>
        private void Publish(ShopSettings settings)
        {
            lock (_gate)
                _current = settings;

            Vat.Configure(settings.VatPercent / 100m);
            StockLevels.Configure(settings.LowStockPieces, settings.LowStockMetres);
            DocumentPdfBuilder.Configure(settings);
        }

        /// <summary>
        /// A missing file is the ordinary case on a fresh install and means
        /// the defaults, which are the figures the code held before this
        /// screen existed. A file that cannot be read is logged and treated
        /// the same way: the shop opening with 12% VAT and a 5-piece warning
        /// is better than the shop not opening.
        /// </summary>
        private ShopSettings Read()
        {
            try
            {
                if (!File.Exists(_path))
                    return new ShopSettings();

                var json = File.ReadAllText(_path);

                return JsonSerializer.Deserialize<ShopSettings>(json) ?? new ShopSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read the shop settings from {Path}; using the defaults.", _path);
                return new ShopSettings();
            }
        }
    }
}
