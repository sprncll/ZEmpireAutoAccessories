using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Inventory")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index(string? status, string? q)
        {
            var stockLevels = await _inventoryService.GetStockLevels();

            const decimal lowStockThreshold = 5;

            // Counts come off the whole list, before any filtering, so the
            // tabs still say how much needs attention while you are looking
            // at one of them.
            ViewData["CountAll"] = stockLevels.Count;
            ViewData["CountOut"] = stockLevels.Count(s => (s.StockOnHand ?? 0) <= 0);
            ViewData["CountLow"] = stockLevels.Count(s =>
                (s.StockOnHand ?? 0) > 0 && (s.StockOnHand ?? 0) <= lowStockThreshold);
            ViewData["CountIn"] = stockLevels.Count(s => (s.StockOnHand ?? 0) > lowStockThreshold);

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
                    "OutOfStock" => stockLevels.Where(s => (s.StockOnHand ?? 0) <= 0).ToList(),
                    "LowStock" => stockLevels.Where(s => (s.StockOnHand ?? 0) > 0 && (s.StockOnHand ?? 0) <= lowStockThreshold).ToList(),
                    "InStock" => stockLevels.Where(s => (s.StockOnHand ?? 0) > lowStockThreshold).ToList(),
                    _ => stockLevels
                };
            }

            ViewData["Status"] = status;
            ViewData["Search"] = q;

            // Which rows are roll goods, so the list can show "12.50 m"
            // instead of "1250" and offer cm/in/m on the row's move form.
            ViewData["SoldByLength"] =
                await _inventoryService.SoldByLength(stockLevels.Select(s => s.ProductID));

            return View(stockLevels);
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