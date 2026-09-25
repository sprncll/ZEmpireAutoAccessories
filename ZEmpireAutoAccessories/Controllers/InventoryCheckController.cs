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
    /// Counts are per product, not per tint variant, because stock itself is
    /// tracked per product - inv.InventoryTransaction has no TintVariantID, so
    /// a variant-level count would have nothing to reconcile against.
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
            List<string> unit)
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
                .OrderBy(p => p.Category.CategoryName)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            return products
                .Select(p => new StockCountRow
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    SystemStock = stock.TryGetValue(p.ProductID, out var s) ? s : 0,
                    SoldByLength = UnitOfMeasure.IsSoldByLength(p.SoldByLength, p.Category.CategoryName)
                })
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

            return check.Details
                .Select(d => new StockVarianceRow
                {
                    SoldByLength = roll.Contains(d.ProductID),
                    ProductID = d.ProductID,
                    ProductName = names.TryGetValue(d.ProductID, out var n) ? n : $"Product {d.ProductID}",
                    PhysicalStock = d.PhysicalStock,
                    SystemStock = stock.TryGetValue(d.ProductID, out var s) ? s : 0,
                    Unit = d.Unit,
                    StockLevel = d.StockLevel
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
