using System.Text.Json;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Which products come off a roll and which are counted in pieces.
    ///
    /// Until now this followed the category NAME: anything filed under
    /// "Window Tint" or "Paint Protection Film" was measured, everything else
    /// counted. That holds for the shelves the shop started with, but it has
    /// two gaps. A category someone adds today - vinyl wrap, say - can only
    /// be made roll goods by editing appsettings.json. And a category is not
    /// always uniform: a boxed pre-cut kit sits among the film but is sold as
    /// one piece.
    ///
    /// cat.Product has no column to say so and nothing may be added to the
    /// schema, so the answers live in a JSON file beside the cut sizes and are
    /// pushed into UnitOfMeasure, which is what the rest of the app asks.
    /// A product beats its category; a category beats the name list; a product
    /// in neither behaves exactly as it did before this file existed.
    ///
    /// Committed to git like the cut sizes, and keyed by the database's own
    /// ids for the same reason - it travels with the backup or not at all.
    ///
    /// Registered as a singleton: the cache is the point, and the lock makes
    /// it safe to share.
    /// </summary>
    public class MeasureStore : IMeasureStore
    {
        private readonly string _path;
        private readonly ILogger<MeasureStore> _logger;
        private readonly object _gate = new();

        private Dictionary<int, bool> _products = new();
        private Dictionary<int, bool> _categories = new();

        public MeasureStore(IConfiguration config, IWebHostEnvironment environment, ILogger<MeasureStore> logger)
        {
            _logger = logger;

            var configured = config["Inventory:MeasureFile"];

            _path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "product-measures.json")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(environment.ContentRootPath, configured);

            Load();
        }

        public bool? ForProduct(int productId)
        {
            lock (_gate)
                return _products.TryGetValue(productId, out var set) ? set : null;
        }

        public bool? ForCategory(int categoryId)
        {
            lock (_gate)
                return _categories.TryGetValue(categoryId, out var set) ? set : null;
        }

        public Task SetProduct(int productId, bool? soldByLength) =>
            Mutate(p =>
            {
                if (soldByLength is { } set)
                    p.Products[productId] = set;
                else
                    p.Products.Remove(productId);
            });

        public Task SetCategory(int categoryId, bool? soldByLength) =>
            Mutate(p =>
            {
                if (soldByLength is { } set)
                    p.Categories[categoryId] = set;
                else
                    p.Categories.Remove(categoryId);
            });

        public Task ForgetProduct(int productId) =>
            Mutate(p => p.Products.Remove(productId));

        public Task ForgetCategory(int categoryId) =>
            Mutate(p => p.Categories.Remove(categoryId));

        /// <summary>
        /// Applies a change and rewrites the file whole. There is no partial
        /// state to get wrong, and the whole table is small enough that
        /// rewriting it costs nothing.
        /// </summary>
        private async Task Mutate(Action<(Dictionary<int, bool> Products, Dictionary<int, bool> Categories)> change)
        {
            Dictionary<int, bool> products;
            Dictionary<int, bool> categories;

            lock (_gate)
            {
                products = new Dictionary<int, bool>(_products);
                categories = new Dictionary<int, bool>(_categories);
            }

            change((products, categories));

            var json = JsonSerializer.Serialize(
                new StoredMeasures
                {
                    Categories = categories
                        .OrderBy(e => e.Key)
                        .Select(e => new StoredCategory { CategoryID = e.Key, SoldByLength = e.Value })
                        .ToList(),
                    Products = products
                        .OrderBy(e => e.Key)
                        .Select(e => new StoredProduct { ProductID = e.Key, SoldByLength = e.Value })
                        .ToList()
                },
                new JsonSerializerOptions { WriteIndented = true });

            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Written to one side and moved into place, so a process that dies
            // mid-write leaves the old file intact rather than a half one.
            var temporary = _path + ".tmp";
            await File.WriteAllTextAsync(temporary, json);
            File.Move(temporary, _path, overwrite: true);

            Publish(products, categories);
        }

        private void Load()
        {
            var (products, categories) = Read();
            Publish(products, categories);
        }

        /// <summary>
        /// Takes the new tables as the cache and hands them to UnitOfMeasure,
        /// which is where every screen reads them from. Both happen under the
        /// lock so the cache and what the app answers never disagree.
        /// </summary>
        private void Publish(Dictionary<int, bool> products, Dictionary<int, bool> categories)
        {
            lock (_gate)
            {
                _products = products;
                _categories = categories;
                UnitOfMeasure.ConfigureOverrides(products, categories);
            }
        }

        private (Dictionary<int, bool> Products, Dictionary<int, bool> Categories) Read()
        {
            if (!File.Exists(_path))
                return (new Dictionary<int, bool>(), new Dictionary<int, bool>());

            try
            {
                var stored = JsonSerializer.Deserialize<StoredMeasures>(File.ReadAllText(_path))
                             ?? new StoredMeasures();

                return (
                    stored.Products
                        .GroupBy(p => p.ProductID)
                        .ToDictionary(g => g.Key, g => g.Last().SoldByLength),
                    stored.Categories
                        .GroupBy(c => c.CategoryID)
                        .ToDictionary(g => g.Key, g => g.Last().SoldByLength));
            }
            catch (Exception ex)
            {
                // An unreadable file must not pass silently as an empty one:
                // empty means every product falls back to its category name,
                // which would put a measured product back on a piece count and
                // deduct 1 cm from a roll for a whole windshield. Say so.
                _logger.LogError(ex,
                    "Product measures at {Path} could not be read. Every product falls back to its category.", _path);

                return (new Dictionary<int, bool>(), new Dictionary<int, bool>());
            }
        }

        /// <summary>The on-disk shape, kept separate from the view models.</summary>
        private sealed class StoredMeasures
        {
            public List<StoredCategory> Categories { get; set; } = new();
            public List<StoredProduct> Products { get; set; } = new();
        }

        private sealed class StoredCategory
        {
            public int CategoryID { get; set; }
            public bool SoldByLength { get; set; }
        }

        private sealed class StoredProduct
        {
            public int ProductID { get; set; }
            public bool SoldByLength { get; set; }
        }
    }
}
