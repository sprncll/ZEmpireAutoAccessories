using System.Text.Json;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// What each completed document took off the shelf.
    ///
    /// Stock is posted from the cut sizes, and the cut sizes are figures
    /// somebody maintains - they get corrected as real jobs are measured. So
    /// the length a job order took in March is not necessarily the length the
    /// table would give for it today, and a document reversed after a
    /// correction would put back a different amount than it took. The shelf
    /// would drift by the difference, quietly, in a system whose whole purpose
    /// is to say what is on the shelf. Worse, clearing a cut size made the
    /// reversal throw, leaving a document stuck Completed with its film
    /// stranded and a message telling the user to complete it again.
    ///
    /// So the posting is written down: reversing reads what was taken rather
    /// than working it out afresh. inv.InventoryTransaction has no link back
    /// to the document that caused a movement and no column to put one in, so
    /// this is a file, beside the cut sizes and the payment proofs.
    ///
    /// A document completed before this existed has nothing on record; the
    /// caller falls back to recomputing, which is what it did before.
    /// </summary>
    public class StockPostingLog : IStockPostingLog
    {
        private readonly string _path;
        private readonly ILogger<StockPostingLog> _logger;
        private readonly object _gate = new();

        private Dictionary<string, List<Entry>>? _cache;

        public StockPostingLog(IConfiguration config, IWebHostEnvironment environment, ILogger<StockPostingLog> logger)
        {
            _logger = logger;

            var configured = config["Inventory:StockPostingFile"];

            _path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "stock-postings.json")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);
        }

        public IReadOnlyCollection<DocumentStockLine>? Taken(DocumentKind kind, int documentId)
        {
            lock (_gate)
            {
                return Cache().TryGetValue(Key(kind, documentId), out var entries)
                    ? entries.Select(e => new DocumentStockLine(e.ProductID, e.Quantity, e.Unit)).ToList()
                    : null;
            }
        }

        public Task Record(DocumentKind kind, int documentId, IEnumerable<DocumentStockLine> lines) =>
            Write(cache => cache[Key(kind, documentId)] = lines
                .Select(l => new Entry { ProductID = l.ProductID, Quantity = l.Quantity, Unit = l.Unit })
                .ToList());

        public Task Clear(DocumentKind kind, int documentId) =>
            Write(cache => cache.Remove(Key(kind, documentId)));

        private static string Key(DocumentKind kind, int documentId) => $"{kind}-{documentId}";

        private async Task Write(Action<Dictionary<string, List<Entry>>> change)
        {
            string json;

            lock (_gate)
            {
                var cache = Cache();
                change(cache);
                json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // To one side and moved into place, so a process that dies part
            // way through leaves the previous record rather than a half file.
            var temporary = _path + ".tmp";
            await File.WriteAllTextAsync(temporary, json);
            File.Move(temporary, _path, overwrite: true);
        }

        private Dictionary<string, List<Entry>> Cache() => _cache ??= Read();

        private Dictionary<string, List<Entry>> Read()
        {
            if (!File.Exists(_path))
                return new Dictionary<string, List<Entry>>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, List<Entry>>>(File.ReadAllText(_path))
                       ?? new Dictionary<string, List<Entry>>();
            }
            catch (Exception ex)
            {
                // Treated as nothing on record, which falls back to
                // recomputing - the behaviour before this file existed.
                _logger.LogError(ex, "Stock postings at {Path} could not be read.", _path);
                return new Dictionary<string, List<Entry>>();
            }
        }

        private sealed class Entry
        {
            public int ProductID { get; set; }
            public decimal Quantity { get; set; }
            public string? Unit { get; set; }
        }
    }
}
