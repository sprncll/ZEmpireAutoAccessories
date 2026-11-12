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
        private readonly IShadeStockService _shadeStock;
        private readonly ApplicationDbContext _context;

        public InventoryController(
            IInventoryService inventoryService,
            IShadeStockService shadeStock,
            ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _shadeStock = shadeStock;
            _context = context;
        }

        // GET: Inventory/Shades
        //
        // What is left of each shade. The stock ledger cannot say - it records
        // a product and nothing finer - so this works it out from the last
        // stock count and the document lines since. See ShadeStockService.
        public async Task<IActionResult> Shades()
        {
            return View(await _shadeStock.Build());
        }

        public async Task<IActionResult> Index(string? status, string? q, int? focus = null)
        {
            // The row a movement was just made on, so the list can put the
            // cursor back where it was.
            ViewData["Focus"] = focus;

            var stockLevels = await BuildRows();

            // Counts come off the whole list, before any filtering, so the
            // tabs still say how much needs attention while you are looking
            // at one of them.
            //
            // What counts as low is StockLevels, which the settings screen
            // sets - and which asks a roll in metres and a box in pieces,
            // rather than the single 5 this file used to hold.
            ViewData["CountAll"] = stockLevels.Count;
            ViewData["CountOut"] = stockLevels.Count(s => StockLevels.IsOut(s.StockOnHand));
            ViewData["CountLow"] = stockLevels.Count(s => StockLevels.IsLow(s.StockOnHand, s.SoldByLength));
            ViewData["CountIn"] = stockLevels.Count(s => StockLevels.IsFine(s.StockOnHand, s.SoldByLength));

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
                    "OutOfStock" => stockLevels.Where(s => StockLevels.IsOut(s.StockOnHand)).ToList(),
                    "LowStock" => stockLevels.Where(s => StockLevels.IsLow(s.StockOnHand, s.SoldByLength)).ToList(),
                    "InStock" => stockLevels.Where(s => StockLevels.IsFine(s.StockOnHand, s.SoldByLength)).ToList(),
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
        // The same rows the dashboard counts, from the same place, so a
        // tile and the list it opens cannot disagree about what is low.
        private Task<List<InventoryRow>> BuildRows() => _inventoryService.GetRows();

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
                // Which row it was, so the list can put the cursor back in
                // it. Taking in a delivery is ten movements one after
                // another, and hunting down the same row each time - after a
                // reload that has forgotten where you were - is most of the
                // work.
                ? RedirectToAction(nameof(Index), new { status, q, focus = productId })
                : RedirectToAction(nameof(Details), new { id = productId });

        /// <summary>Product name for the confirmation message.</summary>
        private async Task<string> NameOf(int productId) =>
            (await _inventoryService.GetProduct(productId))?.ProductName ?? "product";

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}