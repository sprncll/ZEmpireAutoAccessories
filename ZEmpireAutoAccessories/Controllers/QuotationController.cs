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
        private readonly ICutSizeStore _cutSizes;
        private readonly ExpiryService _expiry;

        public QuotationController(
            ApplicationDbContext context,
            IQuotationService quotationService,
            IInventoryService inventoryService,
            ICutSizeStore cutSizes,
            ExpiryService expiry)
        {
            _context = context;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
            _cutSizes = cutSizes;
            _expiry = expiry;
        }

        // GET: Quotation?status=Draft&q=...
        //
        // Two states: still being worked on, or converted to a job order. The
        // filter reads the job order rather than the status text, because the
        // quotations converted before the status was written carry whatever
        // they had at the time - see Models/QuotationStatuses.cs.
        public async Task<IActionResult> Index(string? status, string? q, int? page = null)
        {
            // Anything past its Valid Until date reads as Expired before the
            // list is drawn. See ExpiryService.
            await _expiry.Sweep();

            var query = _context.Quotations
                .Include(quotation => quotation.Customer)
                .Include(quotation => quotation.Vehicle)
                .Include(quotation => quotation.JobOrder)
                // Converted is the job order, not the status text. Draft is
                // everything not converted and not put away - a quotation
                // somebody cancelled should not read as live work.
                .Where(quotation =>
                    status == null
                    || (status == QuotationStatuses.Converted
                        ? quotation.JobOrder != null
                        : quotation.JobOrder == null
                          && quotation.Status != "Cancelled"
                          && quotation.Status != "Expired"));

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
                .ToPageAsync(page, ViewData);

            ViewData["Status"] = status;
            ViewData["Search"] = q;
            return View(quotations);
        }

        // GET: Quotation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            await _expiry.Sweep();

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

        // POST: Quotation/QuickCustomer
        //
        // Somebody asking for a price is as likely to be new as somebody
        // buying, and the quotation screen loses what has been typed the same
        // way the sale does. Same panel, same rules - see
        // Services/QuickCustomer - behind this module's own permission rather
        // than the Customers module's, because making a customer is part of
        // making a quotation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCustomer(
            string? lastName, string? firstName, string? contactNumber)
        {
            var result = await Services.QuickCustomer.Add(
                _context, lastName, firstName, contactNumber);

            return result.Error != null
                ? Json(new { error = result.Error })
                : Json(new { id = result.Id, name = result.Name });
        }

        // GET: Quotation/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var quotation = await _quotationService.GetQuotation(id.Value);
            if (quotation == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildQuotationPdf(quotation, _cutSizes);
            return File(pdf, "application/pdf", $"{quotation.QuotationNumber}.pdf");
        }

        // GET: Quotation/Create
        public async Task<IActionResult> Create()
        {
            await LoadHeaderDropdowns();
            return View(new Quotation { QuotationDate = PhTime.Now });
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
            quotation.QuotationDate = PhTime.Now;
            quotation.ValidUntil = DateOnly.FromDateTime(quotation.QuotationDate.AddDays(7));
            quotation.Status = "Draft";
            quotation.QuotationNumber = "PENDING";
            quotation.CreatedAt = PhTime.Now;

            _context.Quotations.Add(quotation);
            await _context.SaveChangesAsync();

            // Number depends on the generated ID, so it's set in a second save.
            quotation.QuotationNumber = DocumentNumber.For(
                DocumentNumber.Quotation, quotation.QuotationID, quotation.QuotationDate);
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

            // A quotation valid until a date that has gone is not valid. An
            // already-expired one keeps its own date, so it can still be
            // opened and saved without the form refusing over a date nobody
            // touched.
            var storedValidUntil = await _context.Quotations
                .AsNoTracking()
                .Where(q => q.QuotationID == id)
                .Select(q => q.ValidUntil)
                .FirstOrDefaultAsync();

            if (quotation.ValidUntil < DateOnly.FromDateTime(PhTime.Today)
                && quotation.ValidUntil != storedValidUntil)
            {
                ModelState.AddModelError(nameof(Quotation.ValidUntil),
                    "A quotation can't be set to expire on a date that has already passed.");
            }

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

            // A discount is money off, so it goes the way a price does: a
            // staff member does not set one, and whatever an administrator
            // set stays. The field posts whether or not the form shows it
            // read-only, so the stored figure is put back rather than
            // trusted.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                quotation.DiscountAmount = existing.DiscountAmount;
                ModelState.Remove(nameof(Quotation.DiscountAmount));
            }

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

            // Given more time, an expired quotation is live again - the
            // customer came back, and the shop agreed to honour the price.
            if (existing.Status == "Expired" && existing.JobOrder == null
                && quotation.ValidUntil >= DateOnly.FromDateTime(PhTime.Today))
                existing.Status = QuotationStatuses.Draft;
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
            int? shadeId,
            int? panelId,
            string? description,
            int quantity,
            string unit,
            decimal unitPrice)
        {
            // A line is a product or a service, not both: the form locks
            // one once the other is picked, and this is that rule where the
            // form is not in the way.
            if (productId != null && serviceId != null)
            {
                TempData["LineError"] = "Pick a product or a service for a line, not both.";
                return RedirectToAction(nameof(Details), new { id = quotationId });
            }

            var quotation = await _context.Quotations
                .Include(q => q.Details)
                .Include(q => q.Vehicle)
                .FirstOrDefaultAsync(q => q.QuotationID == quotationId);

            if (quotation == null)
                return NotFound();

            // A service goes on the quotation once. Quoting the fitting twice
            // is a quantity of two on the one line, not two lines - unlike a
            // product, there is no second price for labour to justify a
            // separate one. Adding it again is a slip, so it is said out loud
            // rather than quietly folded into the document.
            if (serviceId != null)
            {
                var already = await _context.QuotationDetails
                    .Include(d => d.Service)
                    .FirstOrDefaultAsync(d =>
                        d.QuotationID == quotationId && d.ServiceID == serviceId);

                if (already != null)
                {
                    TempData["LineError"] =
                        $"\"{already.Service?.ServiceName}\" is already on this quotation "
                        + $"at a quantity of {already.Quantity:0.##}. "
                        + "Change that line's quantity rather than adding it again.";

                    return RedirectToAction(nameof(Details), new { id = quotationId });
                }

                // And one job is all it ever is. The same fitting done twice is a
                // second visit, not a quantity of two on one line - the price of
                // the work would be multiplied by it, which is what turned a 500-
                // peso fitting into a line reading 1,500. The Qty box is off the
                // screen for a service; this is what makes that true of a posted
                // form as well.
                quantity = 1;
            }

            if (quantity > 0 && unitPrice >= 0 && (productId != null || serviceId != null))
            {
                // Record which exact Pricing row (Product x Tint Variant x
                // Vehicle Classification x Panel) this line's price came from,
                // when the Add Line Item form resolved one client-side.
                int? pricingId = null;
                decimal? bookPrice = null;
                if (productId != null && panelId != null)
                {
                    var priced = await _context.Pricings
                        .Where(p =>
                            p.ProductID == productId &&
                            p.TintVariantID == tintVariantId &&
                            p.VehicleClassificationID == quotation.Vehicle.VehicleClassificationID &&
                            p.PanelID == panelId)
                        .Select(p => new { p.PricingID, p.Price })
                        .FirstOrDefaultAsync();

                    if (priced != null)
                    {
                        pricingId = priced.PricingID;
                        bookPrice = priced.Price;
                    }
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

                // Staff quote from the book; administrators set what the book
                // says. The form shows the price read-only to a staff member,
                // but a form is only a suggestion - the posted figure is
                // discarded here and the book's own price used, so typing over
                // a read-only box, or posting the form by hand, changes
                // nothing. The same rule the job order has.
                //
                // A combination the book has no price for is the one case a
                // staff member may still type: refusing it would mean a
                // quotation nobody can raise until an administrator is found.
                if (!AppRoles.CanEditCatalogPrices(User) && bookPrice is { } fromBook && fromBook > 0)
                    unitPrice = fromBook;

                // The variant and the shade say what was FITTED. They are not
                // price drivers - the shade never was, and a variant only
                // narrows which matrix row applies. Tying them to pricingId
                // meant a film with no price row for this class of vehicle had
                // both silently thrown away, so the job sheet could not say
                // what went on the car. Keep them when they really belong to
                // the product, and let the price fend for itself.
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

                // A panel is what the line COVERS, not what it costs. Tying
                // it to pricingId meant a film with no price row for this
                // class of vehicle had its panel thrown away too, so the line
                // reached the job sheet naming the film and nothing else -
                // not which glass it was cut for. Keep any real panel and let
                // the price fend for itself, exactly as the variant and the
                // shade above.
                int? keptPanelId = null;
                if (productId != null && panelId != null
                    && await _context.Panels.AnyAsync(p => p.PanelID == panelId))
                {
                    keptPanelId = panelId;
                }

                var lineUnit = string.IsNullOrWhiteSpace(unit) ? "Unit" : unit;

                // Tint and film are cut per panel: what leaves the roll is that
                // panel's cut size for this class of vehicle, from inventory.
                // A film line with no panel has nothing to cut, so it is not
                // taken. The form asks for the panel; this holds without it.
                if (productId != null && keptPanelId == null
                    && (await _inventoryService.SoldByLength(new[] { productId.Value })).Count > 0)
                {
                    TempData["LineError"] = "Pick the panel this tint or film is for. "
                        + "What comes off the roll is worked out from the panel's cut size, so there is no quantity to enter.";
                    return RedirectToAction(nameof(Details), new { id = quotationId });
                }

                // A panel of film is ONE panel. What it takes off the roll
                // is the cut size for that panel on this class of vehicle,
                // worked out when the document is completed - DocumentStock
                // multiplies the cut size by this quantity, so a 2 here is a
                // second panel, not a longer cut, and it doubles the price
                // with it. Nobody should be typing into that box, so the form
                // no longer shows it for these lines; this is the same rule
                // kept where JavaScript cannot be switched off.
                if (keptPanelId != null && productId != null
                    && (await _inventoryService.SoldByLength(new[] { productId.Value })).Count > 0)
                {
                    quantity = 1;
                    lineUnit = UnitOfMeasure.PanelUnit;
                }

                // The same PRODUCT added twice is one line of two, not two
                // lines of one - the job order's rule, and a quotation a
                // customer reads should add up the same way. Three rows
                // reading "Armor Rear Side Step" at 1, 1 and 4 tell nobody
                // anything the single row "6" does not. A service never
                // reaches here; it is refused above.
                //
                // Only a line that matches in every respect is added to:
                // same product, same variant, shade and panel, same unit and
                // the same price. A different price is a different deal and
                // has to stay its own line.
                var existingLine = await _context.QuotationDetails
                    .FirstOrDefaultAsync(d =>
                        d.QuotationID == quotationId &&
                        d.ProductID == productId &&
                        d.ServiceID == serviceId &&
                        d.TintVariantID == keptVariantId &&
                        d.ShadeID == keptShadeId &&
                        d.PanelID == keptPanelId &&
                        d.Unit == lineUnit &&
                        d.UnitPrice == unitPrice);

                if (existingLine != null)
                {
                    existingLine.Quantity += quantity;

                    // A description typed on the second go is kept only where
                    // the first had none, so adding to a line cannot quietly
                    // rewrite what it says.
                    if (string.IsNullOrWhiteSpace(existingLine.Description)
                        && !string.IsNullOrWhiteSpace(description))
                        existingLine.Description = description;

                    TempData["Success"] =
                        $"Added {quantity} to the existing line - it now reads {existingLine.Quantity}.";
                }
                else
                {
                    _context.QuotationDetails.Add(new QuotationDetail
                    {
                        QuotationID = quotationId,
                        ProductID = productId,
                        ServiceID = serviceId,
                        TintVariantID = keptVariantId,
                        ShadeID = keptShadeId,
                        PanelID = keptPanelId,
                        PricingID = pricingId,
                        Description = description,
                        Quantity = quantity,
                        Unit = lineUnit,
                        UnitPrice = unitPrice
                    });
                }

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

            // A job order's number is JO-year-sequence, and the sequence is
            // the row's own ID - which does not exist until the row does. So
            // the stored procedure is handed a placeholder and the real
            // number is written straight after, the same two steps
            // JobOrder.Create takes. A job order raised here and one raised
            // in the Job Order module now read the same way; converting used
            // to leave the timestamp behind as the number itself, which is
            // where JO-20261112122715547 came from.
            //
            // Millisecond precision on the placeholder, not just seconds -
            // two conversions landing in the same second (two staff, or a
            // double-click) would otherwise collide against the database's
            // unique constraint on the column.
            var jobOrderNumber = $"JO-{PhTime.Now:yyyyMMddHHmmssfff}";

            try
            {
                var jobOrderId = await _quotationService.ConvertToJobOrder(id, CurrentUserId, jobOrderNumber);

                // Numbered in a second save, and deliberately forgiving: the
                // job order is already committed by here, so a failure
                // writing the number is not a failed conversion. It would
                // leave the placeholder on screen, which is ugly and still
                // unique - reporting a failure would send somebody to convert
                // again and be told it had already happened.
                try
                {
                    var created = await _context.JobOrders.FindAsync(jobOrderId);
                    if (created != null)
                    {
                        jobOrderNumber = DocumentNumber.For(
                            DocumentNumber.JobOrder, created.JobOrderID, created.JobOrderDate);

                        created.JobOrderNumber = jobOrderNumber;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // Left with the placeholder it was created under.
                }

                // dbo.ConvertQuotationToJobOrder is the shop's own procedure
                // and not mine to change, and what it copies onto a job order
                // line is whatever it was written to copy. If it does not
                // carry the tint variant, the shade and the panel across,
                // the job sheet for a converted quotation cannot say which
                // film went on which glass - the one thing the line is for.
                //
                // So they are carried here instead, and only where the job
                // order line is plainly the quotation line it came from:
                // same product or service, same price, same quantity, and
                // the same number of each on both sides, matched in the
                // order they were written. Anything less certain is left
                // alone rather than guessed at. Where the procedure already
                // copies them, this finds nothing to do.
                try
                {
                    await CarryFilmDetailsToJobOrder(id, jobOrderId);
                }
                catch (Exception)
                {
                    // The job order stands either way.
                }

                TempData["Success"] =
                    $"Converted to Job Order {jobOrderNumber}. "
                    + "Walk the car before any film is touched.";

                // Stamping the quotation is separate, and deliberately after
                // the message. The job order is already committed by here, so
                // a failure writing this word is not a failed conversion -
                // reporting one would send someone to convert again and be
                // told it had already happened. The screens read the job
                // order anyway; this only saves the next reader a question.
                try
                {
                    quotation.Status = QuotationStatuses.Converted;
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // Left as it was. Display() still reads Converted.
                }

                // Straight to the walkaround, exactly as raising a job order
                // in the Job Order module does. A converted quotation is a
                // car arriving for work like any other, and the condition it
                // arrived in is a fact about this moment: an hour later the
                // film is on and nobody can say whether the scratch was there
                // first. The checklist's Back goes to the job order, and
                // saving it lands there too, so nothing is lost by passing
                // through here.
                return RedirectToAction("ForJobOrder", "VehicleChecklist", new
                {
                    jobOrderId,
                    returnUrl = Url.Action("Details", "JobOrder", new { id = jobOrderId })
                });
            }
            catch (Exception ex)
            {
                TempData["ConvertError"] = $"Conversion failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Carries what the quotation's lines say about film onto the job
        /// order's. See <see cref="ConvertedLines"/> for which lines are
        /// matched and why the careful ones are the only ones touched.
        /// </summary>
        private async Task CarryFilmDetailsToJobOrder(int quotationId, int jobOrderId)
        {
            var fromQuotation = await _context.QuotationDetails
                .Where(d => d.QuotationID == quotationId)
                .ToListAsync();

            var onJobOrder = await _context.JobOrderDetails
                .Where(d => d.JobOrderID == jobOrderId)
                .ToListAsync();

            if (ConvertedLines.Carry(fromQuotation, onJobOrder))
                await _context.SaveChangesAsync();
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
