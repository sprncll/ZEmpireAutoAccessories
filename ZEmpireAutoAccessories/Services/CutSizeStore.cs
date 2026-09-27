using System.Text.Json;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// The cut sizes: how much film each panel takes on each class of
    /// vehicle. A front windshield on a sedan might be 1.2 m; the same panel
    /// on a van is more. Tinting a car is priced per panel, so a line says
    /// "Front Windshield x1" - and without this table, deducting the line's
    /// quantity would take one centimetre off the roll for a job that used
    /// well over a metre.
    ///
    /// cat.Pricing is already keyed on exactly this - product, tint variant,
    /// vehicle classification, panel - but it carries a price and nothing
    /// else, and there is no column or table anywhere for a length. Adding
    /// one is off the table, so the sizes live in a JSON file beside the
    /// payment proofs, and Inventory has a screen to set them. The file is
    /// small, is read once and cached, and is rewritten whole when someone
    /// saves - there is no partial state to get wrong.
    ///
    /// Registered as a singleton: the cache is the point, and the lock makes
    /// it safe to share.
    /// </summary>
    public class CutSizeStore : ICutSizeStore
    {
        private readonly string _path;
        private readonly ILogger<CutSizeStore> _logger;
        private readonly object _gate = new();

        private Dictionary<(int Classification, int Panel), decimal>? _cache;

        public CutSizeStore(IConfiguration config, IWebHostEnvironment environment, ILogger<CutSizeStore> logger)
        {
            _logger = logger;

            var configured = config["Inventory:CutSizeFile"];

            _path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "cut-sizes.json")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);
        }

        public decimal? Centimeters(int vehicleClassificationId, int panelId) =>
            Cache().TryGetValue((vehicleClassificationId, panelId), out var cm) ? cm : null;

        public IReadOnlyCollection<CutSize> All() =>
            Cache()
                .Select(entry => new CutSize
                {
                    VehicleClassificationID = entry.Key.Classification,
                    PanelID = entry.Key.Panel,
                    Centimeters = entry.Value
                })
                .ToList();

        public async Task Save(IEnumerable<CutSize> sizes)
        {
            // A panel that takes no film is not a cut size, it is a blank box
            // on the form. Dropping those is how a size gets removed.
            var kept = sizes
                .Where(s => s.Centimeters > 0m)
                .GroupBy(s => (s.VehicleClassificationID, s.PanelID))
                .ToDictionary(g => g.Key, g => g.Last().Centimeters);

            var json = JsonSerializer.Serialize(
                kept.Select(entry => new StoredCutSize
                {
                    VehicleClassificationID = entry.Key.Item1,
                    PanelID = entry.Key.Item2,
                    Centimeters = entry.Value
                }).OrderBy(s => s.VehicleClassificationID).ThenBy(s => s.PanelID),
                new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Written to one side and moved into place, so a process that dies
            // mid-write leaves the old table intact rather than a half file
            // that would read as "no cut sizes" and block every tint job.
            var temporary = _path + ".tmp";
            await File.WriteAllTextAsync(temporary, json);
            File.Move(temporary, _path, overwrite: true);

            lock (_gate)
                _cache = kept;
        }

        private Dictionary<(int Classification, int Panel), decimal> Cache()
        {
            lock (_gate)
            {
                if (_cache != null)
                    return _cache;

                _cache = Read();
                return _cache;
            }
        }

        private Dictionary<(int Classification, int Panel), decimal> Read()
        {
            if (!File.Exists(_path))
                return new Dictionary<(int Classification, int Panel), decimal>();

            try
            {
                var stored = JsonSerializer.Deserialize<List<StoredCutSize>>(File.ReadAllText(_path))
                             ?? new List<StoredCutSize>();

                return stored
                    .Where(s => s.Centimeters > 0m)
                    .GroupBy(s => (s.VehicleClassificationID, s.PanelID))
                    .ToDictionary(g => g.Key, g => g.Last().Centimeters);
            }
            catch (Exception ex)
            {
                // An unreadable file must not look like an empty one: empty
                // means "nothing configured", which is a thing the screens act
                // on. Say so loudly and treat it as nothing configured, which
                // blocks tint jobs rather than deducting a wrong length.
                _logger.LogError(ex, "Cut sizes at {Path} could not be read.", _path);
                return new Dictionary<(int Classification, int Panel), decimal>();
            }
        }

        /// <summary>The on-disk shape. Kept separate so the file format does
        /// not move every time the view model does.</summary>
        private sealed class StoredCutSize
        {
            public int VehicleClassificationID { get; set; }
            public int PanelID { get; set; }
            public decimal Centimeters { get; set; }
        }
    }
}
