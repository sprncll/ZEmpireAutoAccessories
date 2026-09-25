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
    [ModuleAuthorize("Quotation")]
    public class QuotationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuotationService _quotationService;

        private readonly IInventoryService _inventoryService;

        public QuotationController(
            ApplicationDbContext context,
            IQuotationService quotationService,
            IInventoryService inventoryService)
        {
            _context = context;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
        }

        // GET: Quotation?status=Draft&q=...
        public async Task<IActionResult> Index(string? status, string? q)
        {
            var query = _context.Quotations
                .Include(quotation => quotation.Customer)
                .Include(quotation => quotation.Vehicle)
                .Include(quotation => quotation.JobOrder)
                .Where(quotation => status == null || quotation.Status == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(quotation =>
                    quotation.QuotationNumber.Contains(term) ||
                    quotation.Customer.FullName.Contains(term) ||
                    (quotation.Vehicle.PlateNumber != null && quotation.Vehicle.PlateNumber.Contains(term)));
            }

            var quotations = await query
                .OrderByDescending(quotation => quotation.QuotationDate)
                .ToListAsync();

            ViewData["Status"] = status;
            ViewData["Search"] = q;
            return View(quotations);
        }

        // GET: Quotation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var quotation = await _quotationService.GetQuotation(id.Value);
            if (quotation == null)
                return NotFound();

            await LoadLineDropdowns();

            // Every Pricing row for this quotation's vehicle classification,
            // for the Add Line Item form to look up the real matrix price
            // (Product x Tint Variant x Panel) client-side instead of just
            // the product's flat DefaultPrice.
            ViewData["PricingMatrix"] = await _context.Pricings
                .Where(p => p.VehicleClassificationID == quotation.Vehicle.VehicleClassificationID)
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

            return View(quotation);
        }

        // GET: Quotation/VehiclesForCustomer?customerId=5
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

        // GET: Quotation/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var quotation = await _quotationService.GetQuotation(id.Value);
            if (quotation == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildQuotationPdf(quotation);
            return File(pdf, "application/pdf", $"{quotation.QuotationNumber}.pdf");
        }

        // GET: Quotation/Create
        public async Task<IActionResult> Create()
        {
            await LoadHeaderDropdowns();
            return View(new Quotation { QuotationDate = DateTime.Now });
        }

        // POST: Quotation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerID,VehicleID,JobTypeID,Remarks")] Quotation quotation)
        {
            ModelState.Remove(nameof(Quotation.QuotationNumber));
            ModelState.Remove(nameof(Quotation.UserId));
            ModelState.Remove(nameof(Quotation.Customer));
            ModelState.Remove(nameof(Quotation.Vehicle));
            ModelState.Remove(nameof(Quotation.User));

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(quotation);
                return View(quotation);
            }

            quotation.UserId = CurrentUserId;
            quotation.QuotationDate = DateTime.Now;
            quotation.ValidUntil = DateOnly.FromDateTime(quotation.QuotationDate.AddDays(7));
            quotation.Status = "Draft";
            quotation.QuotationNumber = "PENDING";
            quotation.CreatedAt = DateTime.Now;

            _context.Quotations.Add(quotation);
            await _context.SaveChangesAsync();

            // Number depends on the generated ID, so it's set in a second save.
            quotation.QuotationNumber = $"QT-{quotation.QuotationID:D6}";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = quotation.QuotationID });
        }

        // GET: Quotation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var quotation = await _context.Quotations.FindAsync(id);
            if (quotation == null)
                return NotFound();

            await LoadHeaderDropdowns(quotation);
            return View(quotation);
        }

        // POST: Quotation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("QuotationID,CustomerID,VehicleID,JobTypeID,ValidUntil,DiscountAmount,TaxAmount,Remarks")] Quotation quotation)
        {
            if (id != quotation.QuotationID)
                return NotFound();

            ModelState.Remove(nameof(Quotation.QuotationNumber));
            ModelState.Remove(nameof(Quotation.UserId));
            ModelState.Remove(nameof(Quotation.Customer));
            ModelState.Remove(nameof(Quotation.Vehicle));
            ModelState.Remove(nameof(Quotation.User));

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(quotation);
                return View(quotation);
            }

            var existing = await _context.Quotations
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.QuotationID == id);
            if (existing == null)
                return NotFound();

            // CK_Quotation_Total requires TotalAmount >= 0, so a discount
            // bigger than the lines plus tax is refused by the database. Say
            // so against the Discount field instead of failing on save.
            var lineTotal = existing.Details.Sum(d => d.Quantity * d.UnitPrice);
            if (quotation.DiscountAmount > lineTotal + quotation.TaxAmount)
            {
                ModelState.AddModelError(nameof(Quotation.DiscountAmount),
                    $"Discount can't be more than the quotation total of ₱{(lineTotal + quotation.TaxAmount):N2}.");
                await LoadHeaderDropdowns(quotation);
                return View(quotation);
            }

            existing.CustomerID = quotation.CustomerID;
            existing.VehicleID = quotation.VehicleID;
            existing.JobTypeID = quotation.JobTypeID;
            existing.ValidUntil = quotation.ValidUntil;
            existing.DiscountAmount = quotation.DiscountAmount;
            existing.TaxAmount = quotation.TaxAmount;
            existing.Remarks = quotation.Remarks;

            RecalculateTotals(existing);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Quotation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var quotation = await _context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Vehicle)
                .Include(q => q.JobOrder)
                .FirstOrDefaultAsync(q => q.QuotationID == id);

            if (quotation == null)
                return NotFound();

            return View(quotation);
        }

        // POST: Quotation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var quotation = await _context.Quotations
                .Include(q => q.JobOrder)
                .FirstOrDefaultAsync(q => q.QuotationID == id);

            if (quotation == null)
                return RedirectToAction(nameof(Index));

            if (quotation.JobOrder != null)
            {
                TempData["DeleteError"] =
                    "Can't delete this quotation. It has already been converted to a job order.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Quotations.Remove(quotation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Quotation/AddLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLine(
            int quotationId,
            int? productId,
            int? serviceId,
            int? tintVariantId,
            int? panelId,
            string? description,
            int quantity,
            string unit,
            decimal unitPrice)
        {
            var quotation = await _context.Quotations
                .Include(q => q.Details)
                .Include(q => q.Vehicle)
                .FirstOrDefaultAsync(q => q.QuotationID == quotationId);

            if (quotation == null)
                return NotFound();

            if (quantity > 0 && unitPrice >= 0 && (productId != null || serviceId != null))
            {
                // Record which exact Pricing row (Product x Tint Variant x
                // Vehicle Classification x Panel) this line's price came from,
                // when the Add Line Item form resolved one client-side.
                int? pricingId = null;
                if (productId != null && panelId != null)
                {
                    pricingId = await _context.Pricings
                        .Where(p =>
                            p.ProductID == productId &&
                            p.TintVariantID == tintVariantId &&
                            p.VehicleClassificationID == quotation.Vehicle.VehicleClassificationID &&
                            p.PanelID == panelId)
                        .Select(p => (int?)p.PricingID)
                        .FirstOrDefaultAsync();
                }

                _context.QuotationDetails.Add(new QuotationDetail
                {
                    QuotationID = quotationId,
                    ProductID = productId,
                    ServiceID = serviceId,
                    TintVariantID = pricingId != null ? tintVariantId : null,
                    PanelID = pricingId != null ? panelId : null,
                    PricingID = pricingId,
                    Description = description,
                    Quantity = quantity,
                    Unit = string.IsNullOrWhiteSpace(unit) ? "Unit" : unit,
                    UnitPrice = unitPrice
                });

                await _context.SaveChangesAsync();

                await _context.Entry(quotation).Collection(q => q.Details).LoadAsync();
                RecalculateTotals(quotation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = quotationId });
        }

        // POST: Quotation/RemoveLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLine(int detailId, int quotationId)
        {
            var detail = await _context.QuotationDetails.FindAsync(detailId);
            if (detail != null)
            {
                _context.QuotationDetails.Remove(detail);
                await _context.SaveChangesAsync();

                var quotation = await _context.Quotations
                    .Include(q => q.Details)
                    .FirstOrDefaultAsync(q => q.QuotationID == quotationId);
                if (quotation != null)
                {
                    RecalculateTotals(quotation);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = quotationId });
        }

        // POST: Quotation/SetStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var allowed = new[] { "Draft", "Sent", "Accepted", "Rejected" };
            if (!allowed.Contains(status))
                return BadRequest();

            var quotation = await _context.Quotations.FindAsync(id);
            if (quotation != null)
            {
                quotation.Status = status;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Quotation/Convert/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id)
        {
            var quotation = await _context.Quotations
                .Include(q => q.JobOrder)
                .FirstOrDefaultAsync(q => q.QuotationID == id);

            if (quotation == null)
                return NotFound();

            if (quotation.JobOrder != null)
            {
                TempData["ConvertError"] = "This quotation has already been converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Millisecond precision, not just seconds - two conversions
            // landing in the same second (two staff, or a double-click)
            // would otherwise generate the same number and collide against
            // the database's unique constraint on it.
            var jobOrderNumber = $"JO-{DateTime.Now:yyyyMMddHHmmssfff}";

            try
            {
                var jobOrderId = await _quotationService.ConvertToJobOrder(id, CurrentUserId, jobOrderNumber);
                TempData["Success"] = $"Converted to Job Order #{jobOrderId} ({jobOrderNumber}).";
            }
            catch (Exception ex)
            {
                TempData["ConvertError"] = $"Conversion failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private static void RecalculateTotals(Quotation quotation)
        {
            quotation.SubTotal = quotation.Details.Sum(d => d.Quantity * d.UnitPrice);
            quotation.TotalAmount = quotation.SubTotal - quotation.DiscountAmount + quotation.TaxAmount;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadHeaderDropdowns(Quotation? quotation = null)
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "CustomerID", "FullName", quotation?.CustomerID);

            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.VehicleID,
                    Display = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model + " (" + v.Customer.FullName + ")"
                })
                .ToListAsync();
            ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "Display", quotation?.VehicleID);

            ViewData["JobTypeID"] = new SelectList(
                await _context.JobTypes.OrderBy(j => j.JobTypeName).ToListAsync(),
                "JobTypeID", "JobTypeName", quotation?.JobTypeID);
        }

        // Rendered as plain <option> tags in the view (not asp-items) so each
        // one can carry a data-price attribute - SelectListItem has no
        // attribute bag to hang that off of.
        private async Task LoadLineDropdowns()
        {
            var lineProducts = await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync();
            ViewData["Products"] = lineProducts;

            // Roll goods take cm/in/m on a line instead of a plain count.
            ViewData["RollProducts"] =
                await _inventoryService.SoldByLength(lineProducts.Select(p => p.ProductID));

            // Stock on hand, so a line can say what is available while it is
            // being written. This is the friendly half - the binding check
            // still happens when the document is completed. Same arithmetic
            // as dbo.vw_StockOnHand.
            ViewData["StockByProduct"] = await _context.InventoryTransactions
                .GroupBy(t => t.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Stock = g.Sum(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Stock);
            ViewData["Services"] = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.ServiceName).ToListAsync();
        }
    }
}
