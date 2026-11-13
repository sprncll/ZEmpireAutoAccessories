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
        private readonly IDocumentStockService _documentStock;
        private readonly ICutSizeStore _cutSizes;
        private readonly SchemaStatus _schema;

        public ServiceInvoiceController(
            ApplicationDbContext context,
            IQuotationService quotationService,
            IInventoryService inventoryService,
            IDocumentStockService documentStock,
            ICutSizeStore cutSizes,
            SchemaStatus schema)
        {
            _schema = schema;
            _context = context;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
            _documentStock = documentStock;
            _cutSizes = cutSizes;
        }

        // GET: ServiceInvoice?status=Paid
        public async Task<IActionResult> Index(string? status, string? q, int? page = null)
        {
            var query = _context.ServiceInvoices
                .Include(i => i.Customer)
                .Include(i => i.Vehicle)
                .Include(i => i.JobOrder)
                .Include(i => i.PaymentMode)
                .Where(i => status == null || i.Status == status);

            // The same search the Job Orders list has: the number on the
            // invoice, who it is for, the car, and the job order it came
            // from - which is what a customer at the counter quotes.
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(term) ||
                    i.Customer.FullName.Contains(term) ||
                    (i.Vehicle != null && i.Vehicle.PlateNumber != null && i.Vehicle.PlateNumber.Contains(term)) ||
                    (i.JobOrder != null && i.JobOrder.JobOrderNumber.Contains(term)));
            }

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToPageAsync(page, ViewData);

            ViewData["Status"] = status;
            ViewData["Search"] = q;
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
                    // Without this the shade on a line reads as null, so the
                    // screen quietly dropped it: a line fitted in Freeze
                    // Medium printed on the PDF with its shade and showed on
                    // screen as "Freeze - Full Wrap". The PDF path has always
                    // included it; this is the screen catching up.
                    .ThenInclude(d => d.Shade)
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

            // Every payment against it, newest last, and the modes a payment
            // may come in on - only once the ledger's script has been run.
            if (await _schema.Has(_context, SchemaStatus.Payments))
            {
                var payments = await _context.ServiceInvoicePayments
                    .Where(p => p.ServiceInvoiceID == invoice.ServiceInvoiceID)
                    .Include(p => p.PaymentMode)
                    .OrderBy(p => p.PaymentDate)
                    .ToListAsync();

                var takers = payments.Select(p => p.UserId).Distinct().ToList();
                ViewData["PaymentTakers"] = await _context.Users
                    .Where(u => takers.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName ?? "");
                ViewData["Payments"] = payments;
                ViewData["PayModes"] = PaymentModes.ForSelection(
                    await _context.PaymentModes.ToListAsync(), invoice.PaymentModeID);
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
                    // The category tells the receipt whether the product is
                    // fitted off a roll, and the variant, shade and panel say
                    // exactly what went on the car.
                    .ThenInclude(d => d.Product!)
                        .ThenInclude(p => p.Category)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Service)
                .Include(i => i.Details)
                    .ThenInclude(d => d.TintVariant)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Shade)
                .Include(i => i.Details)
                    .ThenInclude(d => d.Panel)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);

            if (invoice == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildServiceInvoicePdf(invoice, _cutSizes);
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }

        // GET: ServiceInvoice/Create?jobOrderId=5
        public async Task<IActionResult> Create(int? jobOrderId)
        {
            var invoice = new ServiceInvoice { InvoiceDate = PhTime.Now };

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
            invoice.InvoiceDate = PhTime.Now;
            invoice.CreatedAt = PhTime.Now;
            // The work is not finished and nothing has been handed over yet,
            // so a new invoice starts Pending. It becomes Paid when the
            // payment is recorded.
            invoice.Status = "Pending";

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

            return RedirectToAction(nameof(Details), new
            {
                id = invoice.ServiceInvoiceID,
                returnUrl = BackLink.Given(Request)
            });
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

            // A discount is money off, so it goes the way a price does: a
            // staff member does not set one, and whatever an administrator
            // set stays. The field posts whether or not the form shows it
            // read-only, so the stored figure is put back rather than
            // trusted.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                invoice.DiscountAmount = existing.DiscountAmount;
                ModelState.Remove(nameof(ServiceInvoice.DiscountAmount));
            }

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

            var newTotal = lineTotal - invoice.DiscountAmount + invoice.TaxAmount;

            // Only cash is handed over and change given back. A transfer, a
            // GCash payment or a card is taken for the amount due and nothing
            // else, so the tendered amount is the total and the form does not
            // offer to change it.
            //
            // RecordPayment has applied this since exact settlement was
            // introduced, but Edit did not, so the rule could be walked around
            // simply by editing the invoice afterwards: ₱50,000 typed against a
            // ₱42,000 card invoice stored ₱8,000 of change, and the reprinted
            // receipt showed it. The field posts regardless of being read-only
            // on the form, so the value is replaced here rather than trusted.
            var settlesExactly = await SettlesExactly(invoice.PaymentModeID);
            if (settlesExactly)
            {
                invoice.AmountPaid = newTotal;
                ModelState.Remove(nameof(ServiceInvoice.AmountPaid));
            }

            // CK_ServiceInvoice_Change requires ChangeAmount >= 0 and
            // CK_ServiceInvoice_Math requires ChangeAmount = AmountPaid -
            // TotalAmount, so between them the table cannot hold a part
            // payment. Edit is the one place a real tendered amount is typed
            // in, so refuse a short one here - RecalculateTotals used to
            // quietly raise it to the total instead, which meant the field
            // accepted a number and then saved a different one.
            // A part payment is a deposit now, not an error. What is still
            // refused is money that was never handed over: more than the total
            // is change, and that is fine, but a negative is not a payment.
            if (invoice.AmountPaid < 0)
            {
                ModelState.AddModelError(nameof(ServiceInvoice.AmountPaid),
                    "Amount paid can't be negative.");
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
            int? shadeId,
            int? panelId,
            string description,
            decimal quantity,
            string unit,
            decimal unitPrice,
            decimal discountAmount)
        {
            // A line is a product or a service, not both: the form locks
            // one once the other is picked, and this is that rule where the
            // form is not in the way.
            if (productId != null && serviceId != null)
            {
                TempData["LineError"] = "Pick a product or a service for a line, not both.";
                return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
            }

            var invoice = await _context.ServiceInvoices
                .Include(i => i.Details)
                .Include(i => i.Vehicle)
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

            // A service goes on the invoice once. Billing the fitting twice is
            // a quantity of two on the one line, not two lines - unlike a
            // product, there is no second price for labour to justify a
            // separate one. Adding it again is a slip, so it is said out loud
            // rather than quietly folded into the document.
            if (serviceId != null)
            {
                var already = await _context.ServiceInvoiceDetails
                    .Include(d => d.Service)
                    .FirstOrDefaultAsync(d =>
                        d.ServiceInvoiceID == serviceInvoiceId && d.ServiceID == serviceId);

                if (already != null)
                {
                    TempData["LineError"] =
                        $"\"{already.Service?.ServiceName}\" is already on this invoice "
                        + $"at a quantity of {already.Quantity:0.##}. "
                        + "Change that line's quantity rather than adding it again.";

                    return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
                }

                // And one job is all it ever is. The same fitting done twice is a
                // second visit, not a quantity of two on one line - the price of
                // the work would be multiplied by it, which is what turned a 500-
                // peso fitting into a line reading 1,500. The Qty box is off the
                // screen for a service; this is what makes that true of a posted
                // form as well.
                quantity = 1m;
            }

            // The variant and the shade say what was FITTED, so they are kept
            // when they really belong to the product rather than when a price
            // happens to exist for them.
            int? keptVariantId = null;
            if (productId != null && tintVariantId != null
                && await _context.TintVariants.AnyAsync(v =>
                       v.TintVariantID == tintVariantId && v.ProductID == productId))
            {
                keptVariantId = tintVariantId;
            }

            int? keptShadeId = null;
            if (keptVariantId != null && shadeId != null
                && await _context.Shades.AnyAsync(sh =>
                       sh.ShadeID == shadeId && sh.TintVariantID == keptVariantId))
            {
                keptShadeId = shadeId;
            }

            // Staff bill from the book; administrators set what the book
            // says. The form shows the price read-only to a staff member, but
            // a form is only a suggestion - the posted figure is discarded
            // here and the book's own price used, so typing over a read-only
            // box, or posting the form by hand, changes nothing. The same
            // rule the job order and the quotation have.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                decimal? bookPrice = null;

                if (productId != null && panelId != null && invoice.Vehicle != null)
                {
                    bookPrice = await _context.Pricings
                        .Where(p =>
                            p.ProductID == productId &&
                            p.TintVariantID == tintVariantId &&
                            p.VehicleClassificationID == invoice.Vehicle.VehicleClassificationID &&
                            p.PanelID == panelId)
                        .Select(p => (decimal?)p.Price)
                        .FirstOrDefaultAsync();
                }

                // Nothing in the matrix for this combination: fall back to
                // what the catalogue says the thing costs on its own.
                bookPrice ??= serviceId != null
                    ? await _context.Services
                        .Where(x => x.ServiceID == serviceId)
                        .Select(x => (decimal?)x.DefaultPrice)
                        .FirstOrDefaultAsync()
                    : await _context.Products
                        .Where(x => x.ProductID == productId)
                        .Select(x => x.DefaultPrice)
                        .FirstOrDefaultAsync();

                // A combination the book has no price for is the one case a
                // staff member may still type: refusing it would mean an
                // invoice nobody can raise until an administrator is found.
                if (bookPrice is { } fromBook && fromBook > 0)
                    unitPrice = fromBook;

                // A discount is the other way to change what is charged, so
                // it goes the same way: a staff member bills the book price
                // in full and an administrator decides what comes off it.
                discountAmount = 0m;
            }

            var linePanelId = productId != null ? panelId : null;
            var lineUnit = string.IsNullOrWhiteSpace(unit) ? "Unit" : unit;

            // Tint and film are cut per panel: what leaves the roll is that
            // panel's cut size for this class of vehicle, from inventory.
            // A film line with no panel has nothing to cut, so it is not
            // taken. The form asks for the panel; this holds without it.
            if (productId != null && linePanelId == null
                && (await _inventoryService.SoldByLength(new[] { productId.Value })).Count > 0)
            {
                TempData["LineError"] = "Pick the panel this tint or film is for. "
                    + "What comes off the roll is worked out from the panel's cut size, so there is no quantity to enter.";
                return RedirectToAction(nameof(Details), new { id = serviceInvoiceId });
            }

            // A panel of film is ONE panel. What it takes off the roll
            // is the cut size for that panel on this class of vehicle,
            // worked out when the document is completed - DocumentStock
            // multiplies the cut size by this quantity, so a 2 here is a
            // second panel, not a longer cut, and it doubles the price
            // with it. Nobody should be typing into that box, so the form
            // no longer shows it for these lines; this is the same rule
            // kept where JavaScript cannot be switched off.
            if (linePanelId != null && productId != null
                && (await _inventoryService.SoldByLength(new[] { productId.Value })).Count > 0)
            {
                quantity = 1;
                lineUnit = UnitOfMeasure.PanelUnit;
            }

            // A counted product is counted in pieces - "set" and "pair" are no
            // longer offered, and are not taken from an old page either.
            if (productId != null)
                lineUnit = UnitOfMeasure.ForCounted(lineUnit);

            _context.ServiceInvoiceDetails.Add(new ServiceInvoiceDetail
            {
                ServiceInvoiceID = serviceInvoiceId,
                ProductID = productId,
                ServiceID = serviceId,
                TintVariantID = keptVariantId,
                ShadeID = keptShadeId,
                PanelID = linePanelId,
                Description = description,
                Quantity = quantity,
                Unit = lineUnit,
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

            await MoveToStatus(invoice, status);
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ServiceInvoice/RecordPayment
        //
        // Takes what the customer handed over, works out the change and
        // settles the invoice. This is the only way an invoice becomes Paid
        // through the counter, so the tendered amount is always recorded
        // rather than being assumed equal to the total.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int id, decimal amountTendered, int? paymentModeId = null)
        {
            var invoice = await _context.ServiceInvoices
                .Include(i => i.Details)
                .Include(i => i.PaymentMode)
                .FirstOrDefaultAsync(i => i.ServiceInvoiceID == id);

            if (invoice == null)
                return RedirectToAction(nameof(Details), new { id });

            if (invoice.TotalAmount <= 0)
            {
                TempData["StatusError"] = "Add at least one line item before taking payment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (amountTendered <= 0)
            {
                TempData["StatusError"] = "Enter what the customer handed over.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // What is still owed, not the whole total: a customer who left a
            // deposit last week owes the remainder today, and handing over the
            // full total again would record an overpayment that never
            // happened.
            var balance = invoice.TotalAmount - invoice.AmountPaid;

            if (balance <= 0)
            {
                TempData["StatusError"] = "This invoice is already settled in full.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // A transfer, a wallet or a card is charged what it is asked for
            // and gives nothing back, so anything at or above the balance
            // settles it exactly. Below the balance it is a deposit, and falls
            // through untouched - a deposit by transfer is a real thing.
            // What this payment came in on. A deposit by GCash and a balance
            // in cash are two modes on one invoice, so the box on the screen
            // asks; it starts on the invoice's own mode, which is also what is
            // used when nothing is sent. Only a mode the shop still offers is
            // accepted.
            var mode = invoice.PaymentMode;
            if (paymentModeId != null && paymentModeId != invoice.PaymentModeID)
            {
                var chosen = await _context.PaymentModes.FindAsync(paymentModeId.Value);
                if (chosen != null && PaymentModes.ForSelection(new[] { chosen }).Any())
                    mode = chosen;
            }

            if (amountTendered >= balance
                && PaymentModes.SettlesExactly(mode?.PaymentModeName))
                amountTendered = balance;

            var applied = Math.Min(amountTendered, balance);
            var priorPaid = invoice.AmountPaid;

            var paidAfter = invoice.AmountPaid + amountTendered;

            invoice.AmountPaid = paidAfter;

            // Change only once the whole total is covered. Below it there is
            // nothing to give back; there is a balance still owed.
            invoice.ChangeAmount = paidAfter > invoice.TotalAmount
                ? paidAfter - invoice.TotalAmount
                : 0m;

            // The payment itself, dated now, so it counts in today's takings
            // whatever day the invoice was raised. Added before the save below
            // - or before the status change's transaction - so the two are
            // written together or not at all.
            if (await _schema.Has(_context, SchemaStatus.Payments))
            {
                _context.ServiceInvoicePayments.Add(new ServiceInvoicePayment
                {
                    ServiceInvoiceID = invoice.ServiceInvoiceID,
                    PaymentDate = PhTime.Now,
                    Amount = applied,
                    AmountTendered = amountTendered,
                    ChangeAmount = amountTendered - applied,
                    PaymentModeID = mode?.PaymentModeID ?? invoice.PaymentModeID,
                    UserId = CurrentUserId,
                    Kind = ServiceInvoicePayment.KindPayment,
                    Notes = paidAfter < invoice.TotalAmount ? "Down payment"
                          : priorPaid > 0m ? "Balance"
                          : "Full payment"
                });
            }

            if (paidAfter >= invoice.TotalAmount)
            {
                await MoveToStatus(invoice, "Paid");

                if (TempData["StatusError"] == null)
                    TempData["Success"] = invoice.ChangeAmount > 0m
                        ? $"Payment recorded. Change {Money.Peso(invoice.ChangeAmount)}."
                        : "Payment recorded. Invoice settled in full.";
            }
            else
            {
                // Stays Pending, and deliberately does not move stock: the
                // film has not been paid for yet, and MoveToStatus is what
                // posts it. A deposit is a promise, not a settlement.
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Down payment of {Money.Peso(amountTendered)} recorded. "
                    + $"{Money.Peso(invoice.TotalAmount - paidAfter)} still due.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Moves an invoice to a status, posting or releasing its stock when
        /// that crosses the Paid boundary, and saving both together so a
        /// refused stock movement leaves the status where it was.
        /// </summary>
        private async Task MoveToStatus(ServiceInvoice invoice, string status)
        {
            var holdsStock = OwnsItsStock(invoice) && invoice.Status == "Paid";
            var willHoldStock = OwnsItsStock(invoice) && status == "Paid";

            // Money going back out, as a payment of its own with a minus sign,
            // dated now: a refund, or a Paid invoice put back to Pending or
            // Cancelled (which has always cleared what it was paid). The day
            // it was paid keeps its takings and today shows the return, which
            // is what happened at the drawer.
            ServiceInvoicePayment? giveBack = null;
            var leavesPaid = invoice.Status == "Paid" && (status == "Pending" || status == "Cancelled");
            var refunds = status == "Refunded" && invoice.Status != "Refunded";
            if ((leavesPaid || refunds) && await _schema.Has(_context, SchemaStatus.Payments))
            {
                var taken = await _context.ServiceInvoicePayments
                    .Where(p => p.ServiceInvoiceID == invoice.ServiceInvoiceID)
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new { p.Amount, p.PaymentModeID })
                    .ToListAsync();

                var net = taken.Sum(p => p.Amount);
                if (net > 0m)
                {
                    giveBack = new ServiceInvoicePayment
                    {
                        ServiceInvoiceID = invoice.ServiceInvoiceID,
                        PaymentDate = PhTime.Now,
                        Amount = -net,
                        ChangeAmount = 0m,
                        // Back the way the last of it came in.
                        PaymentModeID = taken.First(p => p.Amount > 0m).PaymentModeID,
                        UserId = CurrentUserId,
                        Kind = refunds ? ServiceInvoicePayment.KindRefund : ServiceInvoicePayment.KindReversal,
                        Notes = refunds ? "Refunded" : $"Marked {status}"
                    };
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (holdsStock != willHoldStock)
                    await _documentStock.Post(
                        DocumentKind.ServiceInvoice, invoice.ServiceInvoiceID,
                        willHoldStock, CurrentUserId);

                if (giveBack != null)
                    _context.ServiceInvoicePayments.Add(giveBack);

                // Sent back to be worked on or written off: the tender that
                // was recorded no longer stands. Cleared to nothing, which the
                // constraints now allow and which is what actually happened -
                // before this they had to read as paid in full, because that
                // was the only figure the table could hold.
                if (invoice.Status == "Paid" && (status == "Pending" || status == "Cancelled"))
                {
                    invoice.AmountPaid = 0m;
                    invoice.ChangeAmount = 0m;
                }

                invoice.Status = status;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                await tx.RollbackAsync();
                TempData["StatusError"] = ex.Message;

                // The only stock problem that is fixed somewhere else, so the
                // banner carries a way to get there.
                if (ex is CutSizeMissingException)
                    TempData["StatusErrorFix"] = "CutSize";
            }
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
        /// <summary>
        /// Whether this payment mode is taken for the amount due and nothing
        /// else - everything but cash. Looked up by id because Edit binds the
        /// mode the user just chose, which may not be the one on the invoice.
        /// </summary>
        private async Task<bool> SettlesExactly(int? paymentModeId)
        {
            if (paymentModeId == null)
                return false;

            var name = await _context.PaymentModes
                .Where(m => m.PaymentModeID == paymentModeId)
                .Select(m => m.PaymentModeName)
                .FirstOrDefaultAsync();

            return PaymentModes.SettlesExactly(name);
        }

        private static void RecalculateTotals(ServiceInvoice invoice, bool clampAmountPaid = true)
        {
            invoice.SubTotal = invoice.Details.Sum(d => d.SubTotal);
            invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;

            // AmountPaid is no longer carried up to the total. It was only
            // ever raised because the table could not hold a smaller figure;
            // now that it can, an invoice with a deposit on it keeps the
            // deposit when a line is added, which is what happened.
            _ = clampAmountPaid;

            invoice.ChangeAmount = invoice.AmountPaid > invoice.TotalAmount
                ? invoice.AmountPaid - invoice.TotalAmount
                : 0m;
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

            // Only the modes the shop offers, in the order it offers them,
            // plus whatever this invoice is already on - see
            // Models/PaymentModes.cs.
            var offered = PaymentModes.ForSelection(
                await _context.PaymentModes.ToListAsync(), invoice?.PaymentModeID);

            ViewData["PaymentModeID"] = new SelectList(
                offered, "PaymentModeID", "PaymentModeName", invoice?.PaymentModeID);

            // Which of those are taken for the amount due and nothing else, so
            // the form can fill the tendered amount in and lock it the moment
            // one is chosen. Ids rather than names, so renaming a mode cannot
            // put the screen out of step with the server.
            ViewData["ExactPaymentModes"] = offered
                .Where(m => PaymentModes.SettlesExactly(m.PaymentModeName))
                .Select(m => m.PaymentModeID)
                .ToList();
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

            // The shades each tint variant comes in, so a line can record
            // which one was fitted. Named camelCase deliberately: System.Text.Json
            // keeps anonymous-type property names exactly as written, so
            // "ShadeID" would reach the browser as "ShadeID".
            // Every tint variant a product has, so the picker can offer them whether
            // or not the product is priced per panel for this vehicle. The variant
            // and shade record what was fitted; the matrix below decides the panel
            // and the price. Building the variant list from the matrix meant a film
            // with no price row - BF Film, for one - offered no variant at all, and
            // so no shade either.
            ViewData["VariantsByProduct"] = await _context.TintVariants
                .OrderBy(v => v.VariantName)
                .Select(v => new
                {
                    productId = v.ProductID,
                    tintVariantId = v.TintVariantID,
                    tintVariantName = v.VariantName
                })
                .ToListAsync();

            // Every panel a line can name, one row per panel. A film always
            // has to say which part of the vehicle it covers, and the price
            // matrix cannot be asked for that: a film with no price row for
            // this class of vehicle has no panels in the matrix at all, so the
            // picker offered none and the line went onto the document saying
            // only the product's name. The matrix still decides the price
            // where it has one; this list only decides what can be named.
            ViewData["AllPanels"] = WalkaroundPanels
                .ForPricing(await _context.Panels.ToListAsync())
                .Select(p => new { panelId = p.PanelID, panelName = p.PanelName })
                .ToList();

            ViewData["ShadesByVariant"] = await _context.Shades
                .OrderBy(sh => sh.ShadeName)
                .Select(sh => new
                {
                    tintVariantId = sh.TintVariantID,
                    shadeId = sh.ShadeID,
                    shadeName = sh.ShadeName
                })
                .ToListAsync();
        }
    }
}
