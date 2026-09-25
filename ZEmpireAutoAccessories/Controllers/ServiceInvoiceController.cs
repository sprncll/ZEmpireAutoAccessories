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
    [ModuleAuthorize("Service Invoices")]
    public class ServiceInvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuotationService _quotationService; // GetNextInvoiceNumber lives here
        private readonly IInventoryService _inventoryService;

        public ServiceInvoiceController(
            ApplicationDbContext context,
            IQuotationService quotationService,
            IInventoryService inventoryService)
        {
            _context = context;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
        }

        // GET: ServiceInvoice?status=Paid
        public async Task<IActionResult> Index(string? status)
        {
            var invoices = await _context.ServiceInvoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.JobOrder)
                .Include(i => i.PaymentMode)
                .Where(i => status == null || i.Status == status)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            ViewData["Status"] = status;
            return View(invoices);
        }

        // GET: ServiceInvoice/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.ServiceInvoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.JobOrder)
                .Include(i => i.PaymentMode)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Product)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Details)
                    .ThenInclude(d => d.TintVariant)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Panel)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);

            if (invoice == null)
                return NotFound();

            await LoadLineDropdowns();

            // sales.Warranty.ServiceInvoiceDetailID points back at these lines,
            // so each one can show its cover or offer to create it pre-linked.
            // Nothing stops a line being covered twice, so the newest wins.
            var lineIds = invoice.Details.Select(d => d.ServiceInvoiceDetailID).ToList();
            var covers = lineIds.Count == 0
                ? new List<Warranty>()
                : await _context.Warranties
                    .Where(w => w.ServiceInvoiceDetailID != null && lineIds.Contains(w.ServiceInvoiceDetailID.Value))
                    .OrderByDescending(w => w.WarrantyID)
                    .ToListAsync();

            ViewData["WarrantyByLine"] = covers
                .GroupBy(w => w.ServiceInvoiceDetailID!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // Every Pricing row for this invoice's vehicle classification,
            // for the Add Line Item form to look up the real matrix price
            // (Product x Tint Variant x Panel) client-side instead of just
            // the product's flat DefaultPrice. Empty when there's no vehicle
            // on this invoice, since there's no classification to price by.
            if (invoice.Vehicle == null)
            {
                ViewData["PricingMatrix"] = new List<object>();
            }
            else
            {
                ViewData["PricingMatrix"] = await _context.Pricings
                    .Where(p => p.VehicleClassificationID == invoice.Vehicle.VehicleClassificationID)
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
            }

            return View(invoice);
        }

        // GET: ServiceInvoice/VehiclesForCustomer?customerId=5
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

        // GET: ServiceInvoice/JobOrdersForCustomer?customerId=5
        public async Task<IActionResult> JobOrdersForCustomer(int customerId, int? excludeInvoiceId)
        {
            var jobOrders = await _context.JobOrders
                .Include(j => j.Vehicle)
                .Where(j => j.CustomerID == customerId &&
                            (j.Status == "Completed" || j.Status == "Posted") &&
                            !_context.ServiceInvoices.Any(i =>
                                i.JobOrderID == j.JobOrderID &&
                                (excludeInvoiceId == null || i.ServiceInvoiceID != excludeInvoiceId)))
                .OrderByDescending(j => j.JobOrderDate)
                .Select(j => new
                {
                    value = j.JobOrderID,
                    text = j.JobOrderNumber + " - " + (j.Vehicle.PlateNumber ?? "No Plate")
                })
                .ToListAsync();

            return Json(jobOrders);
        }

        // GET: ServiceInvoice/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.ServiceInvoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.JobOrder)
                .Include(i => i.PaymentMode)
                .Include(i => i.User)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Product)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);

            if (invoice == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildServiceInvoicePdf(invoice);
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }

        // GET: ServiceInvoice/Create?jobOrderId=5
        public async Task<IActionResult> Create(int? jobOrderId)
        {
            var invoice = new ServiceInvoice { InvoiceDate = DateTime.Now };

            if (jobOrderId != null)
            {
                var jobOrder = await _context.JobOrders.FindAsync(jobOrderId.Value);
                if (jobOrder == null)
                    return NotFound();

                if (jobOrder.Status != "Completed" && jobOrder.Status != "Posted")
                {
                    TempData["LockError"] = "Only a Completed or Posted job order can be invoiced.";
                    return RedirectToAction("Details", "JobOrder", new { id = jobOrderId });
                }

                if (await _context.ServiceInvoices.AnyAsync(i => i.JobOrderID == jobOrderId))
                {
                    TempData["LockError"] = "This job order has already been invoiced.";
                    return RedirectToAction("Details", "JobOrder", new { id = jobOrderId });
                }

                invoice.CustomerID = jobOrder.CustomerID;
                invoice.VehicleID = jobOrder.VehicleID;
                invoice.JobOrderID = jobOrder.JobOrderID;
                ViewData["FromJobOrderNumber"] = jobOrder.JobOrderNumber;
            }

            await LoadHeaderDropdowns(invoice);
            return View(invoice);
        }

        // POST: ServiceInvoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerID,VehicleID,JobOrderID,PaymentModeID,Remarks")] ServiceInvoice invoice)
        {
            ModelState.Remove(nameof(ServiceInvoice.InvoiceNumber));
            ModelState.Remove(nameof(ServiceInvoice.UserId));
            ModelState.Remove(nameof(ServiceInvoice.Customer));
            ModelState.Remove(nameof(ServiceInvoice.User));
            ModelState.Remove(nameof(ServiceInvoice.PaymentMode));
            ModelState.Remove(nameof(ServiceInvoice.InvoiceNoSeries));

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(invoice);
                return View(invoice);
            }

            if (invoice.JobOrderID != null && await _context.ServiceInvoices.AnyAsync(i => i.JobOrderID == invoice.JobOrderID))
            {
                TempData["LockError"] = "This job order has already been invoiced.";
                return RedirectToAction("Details", "JobOrder", new { id = invoice.JobOrderID });
            }

            string invoiceNumber;
            int seriesId;
            try
            {
                (invoiceNumber, seriesId) = await _quotationService.GetNextInvoiceNumber(CurrentUserId);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Couldn't generate an invoice number: {ex.Message}");
                await LoadHeaderDropdowns(invoice);
                return View(invoice);
            }

            invoice.InvoiceNumber = invoiceNumber;
            invoice.InvoiceNoSeriesID = seriesId;
            invoice.UserId = CurrentUserId;
            invoice.InvoiceDate = DateTime.Now;
            invoice.CreatedAt = DateTime.Now;
            invoice.Status = "Paid";

            _context.ServiceInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Carry the job order's own line items over so staff don't have
            // to re-enter everything that was already worked out there.
            if (invoice.JobOrderID != null)
            {
                var jobOrderDetails = await _context.JobOrderDetails
                    .Include(d => d.Product)
                    .Include(d => d.Service)
                    .Where(d => d.JobOrderID == invoice.JobOrderID)
                    .ToListAsync();

                foreach (var d in jobOrderDetails)
                {
                    _context.ServiceInvoiceDetails.Add(new ServiceInvoiceDetail
                    {
                        ServiceInvoiceID = invoice.ServiceInvoiceID,
                        ProductID = d.ProductID,
                        ServiceID = d.ServiceID,
                        TintVariantID = d.TintVariantID,
                        ShadeID = d.ShadeID,
                        PanelID = d.PanelID,
                        Description = d.Description ?? d.Product?.ProductName ?? d.Service?.ServiceName ?? "Item",
                        Quantity = d.Quantity,
                        Unit = d.Unit,
                        UnitPrice = d.UnitPrice
                    });
                }

                if (jobOrderDetails.Count > 0)
                {
                    await _context.SaveChangesAsync();

                    // Same load-then-recalc sequence AddLine uses below - reload
                    // the just-saved details onto the tracked invoice instance
                    // itself rather than re-querying a separate instance.
                    await _context.Entry(invoice).Collection(i => i.Details).LoadAsync();
                    RecalculateTotals(invoice);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = invoice.ServiceInvoiceID });
        }

        // GET: ServiceInvoice/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.ServiceInvoices.FindAsync(id);
            if (invoice == null)
                return NotFound();

            await LoadHeaderDropdowns(invoice);
            return View(invoice);
        }

        // POST: ServiceInvoice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ServiceInvoiceID,CustomerID,VehicleID,JobOrderID,PaymentModeID,DiscountAmount,TaxAmount,AmountPaid,Remarks")]
            ServiceInvoice invoice)
        {
            if (id != invoice.ServiceInvoiceID)
                return NotFound();

            ModelState.Remove(nameof(ServiceInvoice.InvoiceNumber));
            ModelState.Remove(nameof(ServiceInvoice.UserId));
            ModelState.Remove(nameof(ServiceInvoice.Customer));
            ModelState.Remove(nameof(ServiceInvoice.User));
            ModelState.Remove(nameof(ServiceInvoice.PaymentMode));
            ModelState.Remove(nameof(ServiceInvoice.InvoiceNoSeries));

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(invoice);
                return View(invoice);
            }

            var existing = await _context.ServiceInvoices
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);
            if (existing == null)
                return NotFound();

            // CK_ServiceInvoice_Total requires TotalAmount >= 0, so a discount
            // bigger than the lines plus tax is refused by the database. Say
            // so against the Discount field instead of failing on save.
            var lineTotal = existing.Details.Sum(d => d.SubTotal);
            if (invoice.DiscountAmount > lineTotal + invoice.TaxAmount)
            {
                ModelState.AddModelError(nameof(ServiceInvoice.DiscountAmount),
                    $"Discount can't be more than the invoice total of ₱{(lineTotal + invoice.TaxAmount):N2}.");
                await LoadHeaderDropdowns(invoice);
                return View(invoice);
            }

            // CK_ServiceInvoice_Change requires ChangeAmount >= 0 and
            // CK_ServiceInvoice_Math requires ChangeAmount = AmountPaid -
            // TotalAmount, so between them the table cannot hold a part
            // payment. Edit is the one place a real tendered amount is typed
            // in, so refuse a short one here - RecalculateTotals used to
            // quietly raise it to the total instead, which meant the field
            // accepted a number and then saved a different one.
            var newTotal = lineTotal - invoice.DiscountAmount + invoice.TaxAmount;
            if (invoice.AmountPaid < newTotal)
            {
                ModelState.AddModelError(nameof(ServiceInvoice.AmountPaid),
                    $"Amount paid can't be less than the invoice total of ₱{newTotal:N2}. " +
                    "This invoice records payment in full plus any change, so it can't hold a part payment.");
                await LoadHeaderDropdowns(invoice);
                return View(invoice);
            }

            existing.CustomerID = invoice.CustomerID;
            existing.VehicleID = invoice.VehicleID;
            existing.JobOrderID = invoice.JobOrderID;
            existing.PaymentModeID = invoice.PaymentModeID;
            existing.DiscountAmount = invoice.DiscountAmount;
            existing.TaxAmount = invoice.TaxAmount;
            existing.AmountPaid = invoice.AmountPaid;
            existing.Remarks = invoice.Remarks;

            // The amount tendered was checked above, so keep it exactly as
            // typed rather than letting the clamp raise it.
            RecalculateTotals(existing, clampAmountPaid: false);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: ServiceInvoice/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var invoice = await _context.ServiceInvoices
                .Include(i => i.Customer)
                .Include(i => i.PaymentMode)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);

            if (invoice == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(invoice);
        }

        // POST: ServiceInvoice/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.ServiceInvoices.FindAsync(id);
            if (invoice == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.ServiceInvoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this invoice. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ServiceInvoice/AddLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLine(
            int serviceInvoiceId,
            int? productId,
            int? serviceId,
            int? tintVariantId,
            int? panelId,
            string description,
            decimal quantity,
            string unit,
            decimal unitPrice,
            decimal discountAmount)
        {
            var invoice = await _context.ServiceInvoices
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == serviceInvoiceId);

            if (invoice == null)
                return NotFound();

            // This used to be one silent `if` - anything it rejected redirected
            // straight back to Details with no line added and nothing said,
            // which reads as the page simply refreshing. Say what was wrong.
            string? lineError = null;
            if (OwnsItsStock(invoice) && invoice.Status == "Paid")
                lineError =
                    "This invoice is paid and its products have already left stock. " +
                    "Set it back to Pending to change its line items.";
            else if (string.IsNullOrWhiteSpace(description))
                lineError = "Description is required for a line item. Picking a product or service fills it in for you.";
            else if (quantity <= 0)
                lineError = "Quantity must be more than zero.";
            else if (unitPrice < 0)
                lineError = "Price can't be negative.";
            else if (discountAmount < 0)
                lineError = "Line discount can't be negative.";
            else if (discountAmount > quantity * unitPrice)
                lineError = $"Line discount can't be more than the line total of ₱{(quantity * unitPrice):N2}.";

            if (lineError != null)
            {
                TempData["LineError"] = lineError;
                return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
            }

            _context.ServiceInvoiceDetails.Add(new ServiceInvoiceDetail
            {
                ServiceInvoiceID = serviceInvoiceId,
                ProductID = productId,
                ServiceID = serviceId,
                TintVariantID = productId != null ? tintVariantId : null,
                PanelID = productId != null ? panelId : null,
                Description = description,
                Quantity = quantity,
                Unit = string.IsNullOrWhiteSpace(unit) ? "Unit" : unit,
                UnitPrice = unitPrice,
                DiscountAmount = discountAmount
            });

            await _context.SaveChangesAsync();

            await _context.Entry(invoice).Collection(i => i.Details).LoadAsync();
            RecalculateTotals(invoice);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
        }

        // POST: ServiceInvoice/RemoveLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLine(int detailId, int serviceInvoiceId)
        {
            // Same reason as AddLine: the stock is already out.
            var owner = await _context.ServiceInvoices.FindAsync(serviceInvoiceId);
            if (owner != null && OwnsItsStock(owner) && owner.Status == "Paid")
            {
                TempData["LineError"] =
                    "This invoice is paid and its products have already left stock. " +
                    "Set it back to Pending to change its line items.";
                return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
            }

            var detail = await _context.ServiceInvoiceDetails.FindAsync(detailId);
            if (detail != null)
            {
                _context.ServiceInvoiceDetails.Remove(detail);
                await _context.SaveChangesAsync();

                var invoice = await _context.ServiceInvoices
                    .Include(i => i.Details)
                    .FirstOrDefaultAsync(i => i.ServiceInvoiceID == serviceInvoiceId);
                if (invoice != null)
                {
                    RecalculateTotals(invoice);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
        }

        // POST: ServiceInvoice/SetStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var allowed = new[] { "Paid", "Pending", "Cancelled", "Refunded" };
            if (!allowed.Contains(status))
                return BadRequest();

            var invoice = await _context.ServiceInvoices.FindAsync(id);
            if (invoice == null)
                return RedirectToAction(nameof(Details), new { id });

            var holdsStock = OwnsItsStock(invoice) && invoice.Status == "Paid";
            var willHoldStock = OwnsItsStock(invoice) && status == "Paid";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (holdsStock != willHoldStock)
                    await _inventoryService.PostDocumentStock(
                        await ProductLines(id), willHoldStock, CurrentUserId);

                invoice.Status = status;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                await tx.RollbackAsync();
                TempData["StatusError"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Whether this invoice is the document that moves its own stock.
        ///
        /// An invoice raised against a job order bills for work the job order
        /// already recorded, and that job order takes the products out of
        /// stock when it is completed. Deducting here as well would count the
        /// same film twice, so only a standalone invoice - one with no
        /// JobOrderID - owns its stock.
        /// </summary>
        private static bool OwnsItsStock(ServiceInvoice invoice) => invoice.JobOrderID == null;

        /// <summary>
        /// The product lines of an invoice, as stock movements. Service-only
        /// lines carry no ProductID and move no stock.
        /// </summary>
        private async Task<IReadOnlyCollection<DocumentStockLine>> ProductLines(int serviceInvoiceId)
        {
            var rows = await _context.ServiceInvoiceDetails
                .Where(d => d.ServiceInvoiceID == serviceInvoiceId && d.ProductID != null)
                .Select(d => new { ProductID = d.ProductID!.Value, d.Quantity, d.Unit })
                .ToListAsync();

            // The line's Unit is what the quantity was written in - "m" or
            // "in" for film, "Unit" for anything counted. The inventory
            // service converts roll goods and passes the rest through.
            return rows
                .Select(r => new DocumentStockLine(r.ProductID, r.Quantity, r.Unit))
                .ToList();
        }

        /// <summary>
        /// Recomputes SubTotal, TotalAmount and ChangeAmount from the lines.
        ///
        /// clampAmountPaid carries the tendered amount up to the new total.
        /// That is right where the total moved on its own - creating the
        /// invoice, adding or removing a line - because nobody has said what
        /// was handed over yet, and CK_ServiceInvoice_Change would reject a
        /// negative change. It is wrong on Edit, where the amount is typed
        /// in: raising it there would save a number the user did not enter.
        /// Edit passes false and validates instead.
        /// </summary>
        private static void RecalculateTotals(ServiceInvoice invoice, bool clampAmountPaid = true)
        {
            invoice.SubTotal = invoice.Details.Sum(d => d.SubTotal);
            invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;

            if (clampAmountPaid && invoice.AmountPaid < invoice.TotalAmount)
            {
                invoice.AmountPaid = invoice.TotalAmount;
            }

            invoice.ChangeAmount = invoice.AmountPaid - invoice.TotalAmount;
        }

        // ServiceInvoice -> ServiceInvoiceDetail cascades, but a Warranty referencing
        // one of those lines is ON DELETE RESTRICT, which blocks the cascade.
        private async Task<string?> BuildBlockReason(int invoiceId)
        {
            var warranties = await _context.Warranties
                .CountAsync(w => w.ServiceInvoiceDetail != null && w.ServiceInvoiceDetail.ServiceInvoiceID == invoiceId);

            if (warranties == 0)
                return null;

            return $"Can't delete this invoice. {warranties} warranty record{(warranties == 1 ? "" : "s")} " +
                   "still reference its line items. Remove those first.";
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadHeaderDropdowns(ServiceInvoice? invoice = null)
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "CustomerID", "FullName", invoice?.CustomerID);

            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.VehicleID,
                    Display = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model + " (" + v.Customer.FullName + ")"
                })
                .ToListAsync();
            ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "Display", invoice?.VehicleID);

            var jobOrders = await _context.JobOrders
                .OrderByDescending(j => j.JobOrderDate)
                .Select(j => new { j.JobOrderID, Display = j.JobOrderNumber })
                .ToListAsync();
            ViewData["JobOrderID"] = new SelectList(jobOrders, "JobOrderID", "Display", invoice?.JobOrderID);

            ViewData["PaymentModeID"] = new SelectList(
                await _context.PaymentModes.OrderBy(p => p.PaymentModeName).ToListAsync(),
                "PaymentModeID", "PaymentModeName", invoice?.PaymentModeID);
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
