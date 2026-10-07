using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Works out what is left of each shade. See Models/ShadeStock.cs for the
    /// reasoning; this is the arithmetic.
    ///
    /// The one judgement worth stating here: a document's DATE is used as the
    /// moment its film left the shelf. Stock actually moves when the document
    /// is completed or paid, and nothing records when that happened -
    /// inv.InventoryTransaction has a date but no link back to the document,
    /// and the posting log has the link but no date. So a job raised before a
    /// count and completed after it is counted as having gone before, and its
    /// film is subtracted twice: once by the count that already missed it, and
    /// again here. That is wrong in the shop's favour - it under-reports what
    /// is left, which sends somebody to look at the rack - and it corrects
    /// itself at the next count.
    /// </summary>
    public class ShadeStockService : IShadeStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICutSizeStore _cutSizes;
        private readonly IInventoryService _inventory;

        public ShadeStockService(
            ApplicationDbContext context,
            ICutSizeStore cutSizes,
            IInventoryService inventory)
        {
            _context = context;
            _cutSizes = cutSizes;
            _inventory = inventory;
        }

        public async Task<List<ShadeStockProduct>> Build()
        {
            // ---- the shades there are, and which product each belongs to ----
            var shades = await _context.Shades
                .Include(s => s.TintVariant)
                .Select(s => new
                {
                    s.TintVariant.ProductID,
                    ProductName = s.TintVariant.Product.ProductName,
                    CategoryID = s.TintVariant.Product.CategoryID,
                    CategoryName = s.TintVariant.Product.Category.CategoryName,
                    s.TintVariantID,
                    s.TintVariant.VariantName,
                    s.ShadeID,
                    s.ShadeName
                })
                .ToListAsync();

            if (shades.Count == 0)
                return new List<ShadeStockProduct>();

            // Only roll goods. A shade on something counted in pieces would
            // have nothing measurable to report.
            var rollProducts = shades
                .Select(s => s.ProductID)
                .Distinct()
                .Where(id =>
                {
                    var any = shades.First(s => s.ProductID == id);
                    return UnitOfMeasure.ForProduct(id, any.CategoryID, any.CategoryName);
                })
                .ToHashSet();

            shades = shades.Where(s => rollProducts.Contains(s.ProductID)).ToList();
            if (shades.Count == 0)
                return new List<ShadeStockProduct>();

            // ---- the baseline: the latest count that recorded each shade ----
            var counts = await _context.InventoryCheckDetails
                .Where(d => d.ShadeID != null && rollProducts.Contains(d.ProductID))
                .Select(d => new
                {
                    d.ProductID,
                    ShadeID = d.ShadeID!.Value,
                    d.PhysicalStock,
                    d.InventoryCheck.CheckDate
                })
                .ToListAsync();

            var baseline = counts
                .GroupBy(c => (c.ProductID, c.ShadeID))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.CheckDate).First());

            // The count a product is measured from is the newest one that
            // recorded any of its shades - a count covers the whole rack, so
            // its shades share a date.
            var countedOn = counts
                .GroupBy(c => c.ProductID)
                .ToDictionary(g => g.Key, g => g.Max(c => c.CheckDate));

            var stock = await StockByProduct();
            var consumption = await Consumption(rollProducts, countedOn);
            var received = await ReceivedSince(rollProducts, countedOn);

            // ---- assemble ----
            return shades
                .GroupBy(s => s.ProductID)
                .Select(g =>
                {
                    var first = g.First();
                    var product = new ShadeStockProduct
                    {
                        ProductID = g.Key,
                        ProductName = first.ProductName,
                        CategoryName = ProductCategories.Canonical(first.CategoryName),
                        SystemStock = stock.TryGetValue(g.Key, out var s) ? s : 0m,
                        CountedOn = countedOn.TryGetValue(g.Key, out var on) ? on : null,
                        UnattributedUsed = consumption.Unattributed.TryGetValue(g.Key, out var u) ? u : 0m,
                        ReceivedSince = received.TryGetValue(g.Key, out var r) ? r : 0m
                    };

                    product.Shades = g
                        .Select(sh => new ShadeStockRow
                        {
                            TintVariantID = sh.TintVariantID,
                            ShadeID = sh.ShadeID,
                            TintVariantName = sh.VariantName,
                            ShadeName = sh.ShadeName,
                            Counted = baseline.TryGetValue((sh.ProductID, sh.ShadeID), out var b)
                                ? b.PhysicalStock
                                : null,
                            CountedOn = baseline.TryGetValue((sh.ProductID, sh.ShadeID), out var b2)
                                ? b2.CheckDate
                                : null,
                            Used = consumption.ByShade.TryGetValue(sh.ShadeID, out var used) ? used : 0m
                        })
                        .OrderBy(r => r.TintVariantName).ThenBy(r => r.ShadeName)
                        .ToList();

                    return product;
                })
                .OrderBy(p => p.CategoryName).ThenBy(p => p.ProductName)
                .ToList();
        }

        /// <summary>
        /// What has been fitted since each product's count, split by shade and
        /// with the shadeless lines kept separately.
        ///
        /// Only documents that actually moved stock are counted, by the same
        /// rules the posting uses: a job order when it is Completed, and a
        /// service invoice when it is Paid AND stands on its own. An invoice
        /// raised from a job order never posts - the job order already did -
        /// so counting it here would subtract the same film twice.
        /// </summary>
        private async Task<(Dictionary<int, decimal> ByShade, Dictionary<int, decimal> Unattributed)>
            Consumption(HashSet<int> products, Dictionary<int, DateTime> countedOn)
        {
            var lines = new List<UsedLine>();

            lines.AddRange(await _context.JobOrderDetails
                .Where(d => d.ProductID != null
                            && products.Contains(d.ProductID.Value)
                            && d.JobOrder.Status == "Completed")
                .Select(d => new UsedLine
                {
                    ProductID = d.ProductID!.Value,
                    ShadeID = d.ShadeID,
                    PanelID = d.PanelID,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    ClassificationID = d.JobOrder.Vehicle.VehicleClassificationID,
                    On = d.JobOrder.JobOrderDate
                })
                .ToListAsync());

            lines.AddRange(await _context.ServiceInvoiceDetails
                .Where(d => d.ProductID != null
                            && products.Contains(d.ProductID.Value)
                            && d.ServiceInvoice.JobOrderID == null
                            && d.ServiceInvoice.Status == "Paid")
                .Select(d => new UsedLine
                {
                    ProductID = d.ProductID!.Value,
                    ShadeID = d.ShadeID,
                    PanelID = d.PanelID,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    ClassificationID = d.ServiceInvoice.Vehicle != null
                        ? d.ServiceInvoice.Vehicle.VehicleClassificationID
                        : (int?)null,
                    On = d.ServiceInvoice.InvoiceDate
                })
                .ToListAsync());

            var byShade = new Dictionary<int, decimal>();
            var unattributed = new Dictionary<int, decimal>();

            foreach (var line in lines)
            {
                // Before the count that this product is measured from, so the
                // count already accounts for it. A product never counted has
                // no baseline, and nothing to measure forward from either.
                if (!countedOn.TryGetValue(line.ProductID, out var from) || line.On <= from)
                    continue;

                var centimeters = Centimeters(line);
                if (centimeters <= 0m)
                    continue;

                if (line.ShadeID is { } shadeId)
                    byShade[shadeId] = (byShade.TryGetValue(shadeId, out var a) ? a : 0m) + centimeters;
                else
                    unattributed[line.ProductID] =
                        (unattributed.TryGetValue(line.ProductID, out var b) ? b : 0m) + centimeters;
            }

            return (byShade, unattributed);
        }

        /// <summary>
        /// Centimetres one line took, by the same rule the posting used: a
        /// panel line takes its cut size times the quantity, anything else
        /// takes the quantity as written, converted from the line's own unit.
        /// A panel with no cut size set contributes nothing rather than
        /// guessing - such a document could not have been completed anyway.
        /// </summary>
        private decimal Centimeters(UsedLine line)
        {
            if (line.PanelID == null || line.ClassificationID == null)
                return UnitOfMeasure.ToBase(line.Quantity, line.Unit);

            var cut = _cutSizes.Centimeters(line.ClassificationID.Value, line.PanelID.Value);
            return cut == null ? 0m : cut.Value * line.Quantity;
        }

        /// <summary>Centimetres received since each product's count.</summary>
        private async Task<Dictionary<int, decimal>> ReceivedSince(
            HashSet<int> products, Dictionary<int, DateTime> countedOn)
        {
            if (countedOn.Count == 0)
                return new Dictionary<int, decimal>();

            var movements = await _context.InventoryTransactions
                .Where(t => products.Contains(t.ProductID) && t.TransactionType == "IN")
                .Select(t => new { t.ProductID, t.Quantity, t.TransactionDate })
                .ToListAsync();

            return movements
                .Where(t => countedOn.TryGetValue(t.ProductID, out var from) && t.TransactionDate > from)
                .GroupBy(t => t.ProductID)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Quantity));
        }

        // Same arithmetic as dbo.vw_StockOnHand and InventoryService.
        private async Task<Dictionary<int, decimal>> StockByProduct() =>
            await _context.InventoryTransactions
                .GroupBy(t => t.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Stock = g.Sum(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Stock);

        private sealed class UsedLine
        {
            public int ProductID { get; init; }
            public int? ShadeID { get; init; }
            public int? PanelID { get; init; }
            public decimal Quantity { get; init; }
            public string? Unit { get; init; }
            public int? ClassificationID { get; init; }
            public DateTime On { get; init; }
        }
    }
}
