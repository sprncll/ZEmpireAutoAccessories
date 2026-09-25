using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Data;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Inventory")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _context;

        public InventoryController(
            IInventoryService inventoryService,
            ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, string? q)
        {
            var stockLevels = await BuildRows();

            const decimal lowStockThreshold = 5;

            // Counts come off the whole list, before any filtering, so the
            // tabs still say how much needs attention while you are looking
            // at one of them.
            ViewData["CountAll"] = stockLevels.Count;
            ViewData["CountOut"] = stockLevels.Count(s => s.StockOnHand <= 0);
            ViewData["CountLow"] = stockLevels.Count(s =>
                s.StockOnHand > 0 && s.StockOnHand <= lowStockThreshold);
            ViewData["CountIn"] = stockLevels.Count(s => s.StockOnHand > lowStockThreshold);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                stockLevels = stockLevels
                    .Where(s => s.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                stockLevels = status switch
                {
                    "OutOfStock" => stockLevels.Where(s => s.StockOnHand <= 0).ToList(),
                    "LowStock" => stockLevels.Where(s => s.StockOnHand > 0 && s.StockOnHand <= lowStockThreshold).ToList(),
                    "InStock" => stockLevels.Where(s => s.StockOnHand > lowStockThreshold).ToList(),
                    _ => stockLevels
                };
            }

            ViewData["Status"] = status;
            ViewData["Search"] = q;

            return View(stockLevels);
        }

        /// <summary>
        /// The list, grouped the way the shelves are: by category, then by
        /// name. Each row knows the unit it is measured in, so the column can
        /// read "4.32 m" or "100 pcs" rather than a bare number whose meaning
        /// depends on the product.
        /// </summary>
        private async Task<List<InventoryRow>> BuildRows()
        {
            var levels = await _inventoryService.GetStockLevels();

            var products = await _context.Products
                .Include(p => p.Category)
                .ToDictionaryAsync(p => p.ProductID, p => new { p.SoldByLength, p.Category.CategoryName });

            return levels
                .Select(l =>
                {
                    var product = products.TryGetValue(l.ProductID, out var pr) ? pr : null;
                    return new InventoryRow
                    {
                        ProductID = l.ProductID,
                        ProductName = l.ProductName,
                        CategoryName = product?.CategoryName ?? "Uncategorised",
                        StockOnHand = l.StockOnHand ?? 0,
                        SoldByLength =
                            UnitOfMeasure.IsSoldByLength(product?.SoldByLength, product?.CategoryName)
                    };
                })
                .OrderBy(r => r.CategoryName)
                .ThenBy(r => r.ProductName)
                .ToList();
        }

        // id = ProductID
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _inventoryService.GetProduct(id.Value);
            if (product == null)
                return NotFound();

            ViewData["StockOnHand"] = await _inventoryService.GetStockOnHand(id.Value);
            ViewData["Transactions"] = await _inventoryService.GetTransactions(id.Value);
            ViewData["SoldByLength"] =
                (await _inventoryService.SoldByLength(new[] { id.Value })).Count > 0;

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(
            int productId, decimal quantity, string? unit, string? returnTo, string? status, string? q)
        {
            try
            {
                await _inventoryService.StockIn(productId, quantity, CurrentUserId, unit);
                TempData["Success"] = $"Stocked in {quantity:N2} {unit ?? "pcs"} of {await NameOf(productId)}.";
            }
            catch (Exception ex)
            {
                TempData["InventoryError"] = ex.Message;
            }

            return Back(productId, returnTo, status, q);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(
            int productId, decimal quantity, string? unit, string? returnTo, string? status, string? q)
        {
            try
            {
                await _inventoryService.StockOut(productId, quantity, CurrentUserId, unit);
                TempData["Success"] = $"Stocked out {quantity:N2} {unit ?? "pcs"} of {await NameOf(productId)}.";
            }
            catch (Exception ex)
            {
                TempData["InventoryError"] = ex.Message;
            }

            return Back(productId, returnTo, status, q);
        }

        /// <summary>
        /// Stock can be moved from the product screen or straight from a row
        /// on the list. Go back to whichever it was, keeping the filter and
        /// search so a run of restocking doesn't lose your place.
        /// </summary>
        private IActionResult Back(int productId, string? returnTo, string? status, string? q) =>
            returnTo == "index"
                ? RedirectToAction(nameof(Index), new { status, q })
                : RedirectToAction(nameof(Details), new { id = productId });

        /// <summary>Product name for the confirmation message.</summary>
        private async Task<string> NameOf(int productId) =>
            (await _inventoryService.GetProduct(productId))?.ProductName ?? "product";

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}