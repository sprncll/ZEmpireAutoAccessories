using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Sales")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISalesService _salesService;
        private readonly IQuotationService _quotationService; // GetNextInvoiceNumber lives here
        private readonly IInventoryService _inventoryService;

        public SalesController(
            ApplicationDbContext context,
            ISalesService salesService,
            IQuotationService quotationService,
            IInventoryService inventoryService)
        {
            _context = context;
            _salesService = salesService;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
        }

        // GET: Sale?q=...&productId=...&dateFrom=...&dateTo=...
        public async Task<IActionResult> Index(string? q, int? productId, DateOnly? dateFrom, DateOnly? dateTo)
        {
            var sales = await _salesService.GetSales(q, productId, dateFrom, dateTo);

            ViewData["Search"] = q;
            ViewData["ProductID"] = new SelectList(
                await _context.Products.OrderBy(p => p.ProductName).ToListAsync(),
                "ProductID", "ProductName", productId);
            ViewData["DateFrom"] = dateFrom;
            ViewData["DateTo"] = dateTo;

            if (productId.HasValue)
                ViewData["UnitsSold"] = await _salesService.GetUnitsSold(productId.Value, dateFrom, dateTo);

            return View(sales);
        }

        // GET: Sale/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _salesService.GetSale(id.Value);
            if (sale == null)
                return NotFound();

            ViewData["WarrantyByLine"] = await LoadWarrantiesForLines(
                sale.SaleDetails.Select(d => d.SalesDetailID).ToList());

            return View(sale);
        }

        /// <summary>
        /// The warranty already covering each sale line, keyed by SalesDetailID,
        /// so the details screen can offer "+ Warranty" on lines without one and
        /// link to the existing record on lines that have one. sales.Warranty
        /// does not stop a line being covered twice, so the newest wins.
        /// </summary>
        private async Task<Dictionary<int, Warranty>> LoadWarrantiesForLines(List<int> salesDetailIds)
        {
            if (salesDetailIds.Count == 0)
                return new Dictionary<int, Warranty>();

            var warranties = await _context.Warranties
                .Where(w => w.SalesDetailID != null && salesDetailIds.Contains(w.SalesDetailID.Value))
                .OrderByDescending(w => w.WarrantyID)
                .ToListAsync();

            return warranties
                .GroupBy(w => w.SalesDetailID!.Value)
                .ToDictionary(g => g.Key, g => g.First());
        }

        // GET: Sale/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _salesService.GetSale(id.Value);
            if (sale == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildSalePdf(sale);
            return File(pdf, "application/pdf", $"{sale.InvoiceNumber}.pdf");
        }

        // GET: Sale/VehiclesForCustomer?customerId=5
        public async Task<IActionResult> VehiclesForCustomer(int customerId)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    value = v.VehicleID,
                    text = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model
                })
                .ToListAsync();

            return Json(vehicles);
        }

        // GET: Sale/PricingForVehicle?vehicleId=5
        // Every Pricing row for this vehicle's classification, so the line-item
        // rows can look up the real matrix price (Product x Tint Variant x
        // Panel) client-side instead of just the product's flat DefaultPrice.
        public async Task<IActionResult> PricingForVehicle(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
                return Json(new List<object>());

            var matrix = await _context.Pricings
                .Where(p => p.VehicleClassificationID == vehicle.VehicleClassificationID)
                .Select(p => new
                {
                    productId = p.ProductID,
                    tintVariantId = p.TintVariantID,
                    tintVariantName = p.TintVariant != null ? p.TintVariant.VariantName : null,
                    panelId = p.PanelID,
                    panelName = p.Panel.PanelName,
                    price = p.Price
                })
                .ToListAsync();

            return Json(matrix);
        }

        // GET: Sale/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        // POST: Sale/Create
        // Bound as parallel flat lists (not List<SaleLineRequest>) since the Create
        // view builds rows client-side with plain repeated input names - simpler
        // than indexed complex-object binding for a small line-item table.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int customerId,
            int? vehicleId,
            int paymentModeId,
            List<int> productId,
            List<int> quantity,
            List<decimal> unitPrice)
        {
            var items = new List<SaleLineRequest>();
            for (int i = 0; i < productId.Count; i++)
            {
                if (productId[i] > 0 && quantity[i] > 0)
                {
                    items.Add(new SaleLineRequest
                    {
                        ProductID = productId[i],
                        Quantity = quantity[i],
                        UnitPrice = unitPrice[i]
                    });
                }
            }

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one line item.");
                await LoadDropdowns();
                return View();
            }

            try
            {
                var (invoiceNumber, _) = await _quotationService.GetNextInvoiceNumber(CurrentUserId);
                var sale = await _salesService.CreateSale(
                    CurrentUserId, customerId, paymentModeId, invoiceNumber, vehicleId, items);

                // Details opens the receipt by itself on this one visit.
                TempData["SaleCompleted"] = sale.InvoiceNumber;
                return RedirectToAction(nameof(Details), new { id = sale.SalesID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadDropdowns();
                return View();
            }
        }

        // GET: Sale/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PaymentMode)
                .FirstOrDefaultAsync(s => s.SalesID == id);

            if (sale == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(sale);
        }

        // POST: Sale/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this sale. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // Sale -> SaleDetail cascades, but a Warranty referencing one of those
        // lines is ON DELETE RESTRICT, which blocks the cascade. Note: deleting a
        // sale does NOT reverse the inventory OUT transactions it posted.
        private async Task<string?> BuildBlockReason(int saleId)
        {
            var warranties = await _context.Warranties
                .CountAsync(w => w.SalesDetail != null && w.SalesDetail.SalesID == saleId);

            if (warranties == 0)
                return null;

            return $"Can't delete this sale. {warranties} warranty record{(warranties == 1 ? "" : "s")} " +
                   "still reference its line items. Remove those first.";
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadDropdowns()
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "CustomerID", "FullName");

            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.VehicleID,
                    Display = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model + " (" + v.Customer.FullName + ")"
                })
                .ToListAsync();
            ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "Display");

            ViewData["PaymentModeID"] = new SelectList(
                await _context.PaymentModes.OrderBy(p => p.PaymentModeName).ToListAsync(),
                "PaymentModeID", "PaymentModeName");

            var saleProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
            ViewData["Products"] = saleProducts;

            // Film and PPF are sold off the roll by the metre, so their lines
            // are counted in metres and their stock reads in metres too.
            ViewData["RollProducts"] =
                await _inventoryService.SoldByLength(saleProducts.Select(p => p.ProductID));

            // Stock on hand per product, so a line can warn while it is being
            // typed instead of the whole sale failing in CreateSale. Same
            // arithmetic as dbo.vw_StockOnHand and the check that service
            // makes - this is the friendly half, not the authority.
            ViewData["StockByProduct"] = await _context.InventoryTransactions
                .GroupBy(t => t.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Stock = g.Sum(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Stock);
        }
    }
}
