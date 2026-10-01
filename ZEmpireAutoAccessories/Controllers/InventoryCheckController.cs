using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// Physical stock counts (inv.InventoryCheck). A count records what was
    /// actually on the shelf; reconciling it posts the difference to
    /// inv.InventoryTransaction so the system figure matches.
    ///
    /// A film that has shades on file is counted one shade at a time.
    /// inv.InventoryCheckDetail has carried TintVariantID and ShadeID all
    /// along, so the sheet can record which shade the metres were on - what
    /// the shop actually wants to know when it is down to one roll of
    /// Superdark.
    ///
    /// The variance stays at PRODUCT level, and reconciling posts at product
    /// level, because inv.InventoryTransaction records a product and nothing
    /// finer: there is one system figure to compare against, so the shade
    /// counts are summed before they are matched to it. The breakdown is a
    /// record of the shelf at that moment, not a running balance - it will
    /// drift until the next count, and the screens say so.
    ///
    /// Shares the Inventory module rather than owning one, so no new
    /// sec.Module row is needed.
    /// </summary>
    [ModuleAuthorize("Inventory")]
    public class InventoryCheckController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;

        // Matches the threshold the Inventory list filters on.
        private const decimal LowStockThreshold = 5;

        public InventoryCheckController(
            ApplicationDbContext context,
            IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // GET: InventoryCheck
        public async Task<IActionResult> Index()
        {
            var checks = await _context.InventoryChecks
                .Include(c => c.User)
                .Include(c => c.Details)
                .OrderByDescending(c => c.CheckDate)
                .ToListAsync();

            return View(checks);
        }

        // GET: InventoryCheck/Create
        public async Task<IActionResult> Create()
        {
            return View(await BuildCountSheet());
        }

        // POST: InventoryCheck/Create
        //
        // Bound as parallel flat lists, the same shape the sale line-item form
        // posts in. A blank count means "not counted" and is skipped, so a
        // partial count of one shelf is a normal thing to record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            List<int> productId,
            List<decimal?> physicalStock,
            List<string> unit,
            List<int?> tintVariantId,
            List<int?> shadeId)
        {
            var counted = new List<InventoryCheckDetail>();
            var roll = await _inventoryService.SoldByLength(productId);

            for (var i = 0; i < productId.Count; i++)
            {
                var physical = i < physicalStock.Count ? physicalStock[i] : null;
                if (physical == null)
                    continue;

                if (physical < 0)
                {
                    // ViewData, not TempData: this renders the sheet again in
                    // the same request rather than redirecting, and TempData
                    // would survive to show once more on the next page.
                    ViewData["CountError"] = "A physical count can't be negative.";
                    return View(await BuildCountSheet());
                }

                var typedUnit = i < unit.Count ? unit[i] : null;
                var byLength = roll.Contains(productId[i]);

                // Roll goods are counted in whatever unit the tape measure
                // reads; store the count in the base unit so it can be
                // compared with stock on hand.
                var counting = byLength
                    ? UnitOfMeasure.ToBase(physical.Value, typedUnit)
                    : physical.Value;

                counted.Add(new InventoryCheckDetail
                {
                    ProductID = productId[i],
                    TintVariantID = i < tintVariantId.Count ? tintVariantId[i] : null,
                    ShadeID = i < shadeId.Count ? shadeId[i] : null,
                    PhysicalStock = counting,
                    // CK on inv.InventoryCheckDetail allows Piece or Roll only,
                    // so the measured unit lives in the figure, not the label.
                    Unit = byLength || typedUnit == "Roll" ? "Roll" : "Piece",
                    StockLevel = LevelFor(counting)
                });
            }

            if (counted.Count == 0)
            {
                ViewData["CountError"] = "Enter a count for at least one product.";
                return View(await BuildCountSheet());
            }

            var check = new InventoryCheck
            {
                UserId = CurrentUserId,
                CheckDate = DateTime.Now
            };

            foreach (var detail in counted)
                check.Details.Add(detail);

            _context.InventoryChecks.Add(check);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = check.InventoryCheckID });
        }

        // GET: InventoryCheck/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var check = await _context.InventoryChecks
                .Include(c => c.User)
                .Include(c => c.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(c => c.InventoryCheckID == id);

            if (check == null)
                return NotFound();

            ViewData["Variances"] = await BuildVariances(check);

            return View(check);
        }

        // POST: InventoryCheck/Reconcile/5
        //
        // Posts the difference between each counted figure and what the system
        // says right now, so stock-on-hand ends up equal to the count. The
        // variance is taken live rather than from when the count was saved,
        // which makes a second run a no-op if nothing moved in between.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reconcile(int id)
        {
            var check = await _context.InventoryChecks
                .Include(c => c.Details)
                .FirstOrDefaultAsync(c => c.InventoryCheckID == id);

            if (check == null)
                return NotFound();

            var variances = await BuildVariances(check);

            var increases = variances
                .Where(v => v.Variance > 0)
                .Select(v => new DocumentStockLine(v.ProductID, v.Variance))
                .ToList();

            var decreases = variances
                .Where(v => v.Variance < 0)
                .Select(v => new DocumentStockLine(v.ProductID, -v.Variance))
                .ToList();

            if (increases.Count == 0 && decreases.Count == 0)
            {
                TempData["CountError"] = "Nothing to adjust - the system already matches this count.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _inventoryService.PostDocumentStock(increases, consume: false, CurrentUserId);
                await _inventoryService.PostDocumentStock(decreases, consume: true, CurrentUserId);
                await tx.CommitAsync();

                TempData["Success"] =
                    $"Stock adjusted for {increases.Count + decreases.Count} product" +
                    $"{(increases.Count + decreases.Count == 1 ? "" : "s")}.";
            }
            catch (InvalidOperationException ex)
            {
                await tx.RollbackAsync();
                TempData["CountError"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Every active product with what the system currently believes it
        /// holds, ready for someone to write the shelf figure beside it.
        /// </summary>
        private async Task<List<StockCountRow>> BuildCountSheet()
        {
            var stock = await StockByProduct();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            // The shades each product's variants come in, so a film can be
            // counted one shade at a time. A product whose variants have no
            // shades on file keeps a single line, which is what every product
            // had before this.
            var shades = await _context.Shades
                .Include(sh => sh.TintVariant)
                .OrderBy(sh => sh.TintVariant.VariantName).ThenBy(sh => sh.ShadeName)
                .Select(sh => new
                {
                    sh.TintVariant.ProductID,
                    sh.TintVariantID,
                    sh.ShadeID,
                    sh.TintVariant.VariantName,
                    sh.ShadeName
                })
                .ToListAsync();

            var shadesByProduct = shades
                .GroupBy(sh => sh.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rows = new List<StockCountRow>();

            foreach (var p in products)
            {
                var byLength = UnitOfMeasure.ForProduct(
                    p.ProductID, p.CategoryID, p.Category.CategoryName);

                var category = ProductCategories.Canonical(p.Category.CategoryName);
                var systemStock = stock.TryGetValue(p.ProductID, out var onHand) ? onHand : 0;

                // Only roll goods are worth splitting. A piece good with tint
                // variants - there are none today, but nothing stops one -
                // would be counted whole, because the shade tells you nothing
                // about a boxed item.
                var split = byLength && shadesByProduct.TryGetValue(p.ProductID, out var list)
                    ? list
                    : null;

                if (split == null)
                {
                    rows.Add(new StockCountRow
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        CategoryName = category,
                        SystemStock = systemStock,
                        SoldByLength = byLength,
                        FirstOfProduct = true,
                        ShadeLineCount = 1
                    });
                    continue;
                }

                for (var i = 0; i < split.Count; i++)
                {
                    rows.Add(new StockCountRow
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        CategoryName = category,
                        // Carried on every line of the product, but only shown
                        // against the first: the system holds one figure for
                        // the product, and repeating it beside each shade would
                        // read as though each shade had that much.
                        SystemStock = systemStock,
                        SoldByLength = byLength,
                        TintVariantID = split[i].TintVariantID,
                        ShadeID = split[i].ShadeID,
                        TintVariantName = split[i].VariantName,
                        ShadeName = split[i].ShadeName,
                        FirstOfProduct = i == 0,
                        ShadeLineCount = split.Count
                    });
                }
            }

            return rows
                // Sorted after the canonical name is resolved, so a shelf with
                // two names still comes out as one contiguous block. The shade
                // lines of a product must stay together and in order, so the
                // product name sorts before anything below it.
                .OrderBy(r => r.CategoryName)
                .ThenBy(r => r.ProductName)
                .ThenByDescending(r => r.FirstOfProduct)
                .ThenBy(r => r.TintVariantName)
                .ThenBy(r => r.ShadeName)
                .ToList();
        }

        /// <summary>
        /// Each counted line against stock-on-hand as it stands now. A
        /// positive variance means the shelf holds more than the system
        /// thought; negative means the system is over-counting.
        /// </summary>
        private async Task<List<StockVarianceRow>> BuildVariances(InventoryCheck check)
        {
            var stock = await StockByProduct();

            var names = await _context.Products
                .Where(p => check.Details.Select(d => d.ProductID).Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID, p => p.ProductName);

            var roll = await _inventoryService.SoldByLength(check.Details.Select(d => d.ProductID));

            // Labels for the shade lines, so the breakdown reads as names
            // rather than ids.
            var shadeIds = check.Details
                .Where(d => d.ShadeID != null)
                .Select(d => d.ShadeID!.Value)
                .Distinct()
                .ToList();

            var shadeLabels = shadeIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Shades
                    .Include(sh => sh.TintVariant)
                    .Where(sh => shadeIds.Contains(sh.ShadeID))
                    .ToDictionaryAsync(
                        sh => sh.ShadeID,
                        sh => sh.TintVariant.VariantName + " - " + sh.ShadeName);

            // GROUPED BY PRODUCT, deliberately. A film counted across three
            // shades is three detail rows, but there is one system figure for
            // it and reconciling posts to inv.InventoryTransaction, which has
            // no shade - so the counts are summed first. One row per detail
            // here would compare each shade against the product's whole stock
            // and post the adjustment three times over.
            return check.Details
                .GroupBy(d => d.ProductID)
                .Select(g => new StockVarianceRow
                {
                    SoldByLength = roll.Contains(g.Key),
                    ProductID = g.Key,
                    ProductName = names.TryGetValue(g.Key, out var n) ? n : $"Product {g.Key}",
                    PhysicalStock = g.Sum(d => d.PhysicalStock),
                    SystemStock = stock.TryGetValue(g.Key, out var s) ? s : 0,
                    Unit = g.First().Unit,
                    // The worst of the group: a product is only Normal when
                    // every shade of it is.
                    StockLevel =
                        g.Any(d => d.StockLevel == "Critical") ? "Critical"
                        : g.Any(d => d.StockLevel == "Low") ? "Low"
                        : "Normal",
                    Shades = g
                        .Where(d => d.ShadeID != null)
                        .Select(d => new StockCountShade
                        {
                            Label = shadeLabels.TryGetValue(d.ShadeID!.Value, out var label)
                                ? label
                                : $"Shade {d.ShadeID}",
                            PhysicalStock = d.PhysicalStock
                        })
                        .OrderBy(x => x.Label)
                        .ToList()
                })
                .OrderBy(v => v.ProductName)
                .ToList();
        }

        // Same arithmetic as dbo.vw_StockOnHand and InventoryService.
        private async Task<Dictionary<int, decimal>> StockByProduct()
        {
            return await _context.InventoryTransactions
                .GroupBy(t => t.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Stock = g.Sum(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Stock);
        }

        // CK on inv.InventoryCheckDetail allows Normal, Low or Critical only.
        // The threshold is a piece count; for roll goods it reads as
        // centimetres, so only the Critical (nothing left) case is meaningful.
        private static string LevelFor(decimal physical) =>
            physical <= 0 ? "Critical"
            : physical <= LowStockThreshold ? "Low"
            : "Normal";

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
