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
    [ModuleAuthorize("Job Orders")]
    public class JobOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IDocumentStockService _documentStock;
        private readonly ICutSizeStore _cutSizes;

        public JobOrderController(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            IDocumentStockService documentStock,
            ICutSizeStore cutSizes)
        {
            _context = context;
            _inventoryService = inventoryService;
            _documentStock = documentStock;
            _cutSizes = cutSizes;
        }

        // GET: JobOrder?status=Pending&q=...
        public async Task<IActionResult> Index(string? status, string? q)
        {
            var query = _context.JobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .Include(j => j.AssignedEmployee)
                .Include(j => j.Quotation)
                .Include(j => j.ServiceInvoices)
                .Where(j => status == null || j.Status == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(j =>
                    j.JobOrderNumber.Contains(term) ||
                    j.Customer.FullName.Contains(term) ||
                    (j.Vehicle.PlateNumber != null && j.Vehicle.PlateNumber.Contains(term)));
            }

            var jobOrders = await query
                .OrderByDescending(j => j.JobOrderDate)
                .ToListAsync();

            ViewData["Status"] = status;
            ViewData["Search"] = q;
            return View(jobOrders);
        }

        // GET: JobOrder/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var jobOrder = await _context.JobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .Include(j => j.JobType)
                .Include(j => j.AssignedEmployee)
                .Include(j => j.Quotation)
                .Include(j => j.ServiceInvoices)
                .Include(j => j.Details)
                    // The category says whether the product comes off a roll,
                    // and the panel says how much of it - see the receipt.
                    .ThenInclude(d => d.Product!)
                        .ThenInclude(p => p.Category)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Service)
                .Include(j => j.Details)
                    .ThenInclude(d => d.TintVariant)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Shade)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Panel)
                .FirstOrDefaultAsync(j => j.JobOrderID == id);

            if (jobOrder == null)
                return NotFound();

            await LoadLineDropdowns();

            // Every Pricing row for this job order's vehicle classification,
            // for the Add Line Item form to look up the real matrix price
            // (Product x Tint Variant x Panel) client-side instead of just
            // the product's flat DefaultPrice.
            ViewData["PricingMatrix"] = await _context.Pricings
                .Where(p => p.VehicleClassificationID == jobOrder.Vehicle.VehicleClassificationID)
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

            return View(jobOrder);
        }

        // GET: JobOrder/VehiclesForCustomer?customerId=5
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

        // GET: JobOrder/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var jobOrder = await _context.JobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .Include(j => j.JobType)
                .Include(j => j.AssignedEmployee)
                .Include(j => j.Details)
                    // The receipt asks the category whether a product comes
                    // off a roll, and the panel how much of it - without
                    // these it silently prints "pc" and no measure.
                    .ThenInclude(d => d.Product!)
                        .ThenInclude(p => p.Category)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Service)
                .Include(j => j.Details)
                    .ThenInclude(d => d.TintVariant)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Shade)
                .Include(j => j.Details)
                    .ThenInclude(d => d.Panel)
                .FirstOrDefaultAsync(j => j.JobOrderID == id);

            if (jobOrder == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildJobOrderPdf(jobOrder, _cutSizes);
            return File(pdf, "application/pdf", $"{jobOrder.JobOrderNumber}.pdf");
        }

        // GET: JobOrder/Create
        public async Task<IActionResult> Create()
        {
            await LoadHeaderDropdowns();

            // Installation is nearly always the day the job is raised, so the
            // box opens on today rather than empty. It stays editable: a job
            // booked for next Tuesday is typed over it, and clearing it is
            // still allowed because the column is nullable.
            return View(new JobOrder
            {
                JobOrderDate = DateTime.Now,
                InstallationDate = DateTime.Today
            });
        }

        // POST: JobOrder/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerID,VehicleID,JobTypeID,AssignedEmployeeID,InstallationDate,ExistingFilmShade,ReasonForChanging,SpecialInstruction,Complaint")]
            JobOrder jobOrder)
        {
            ModelState.Remove(nameof(JobOrder.JobOrderNumber));
            ModelState.Remove(nameof(JobOrder.UserId));
            ModelState.Remove(nameof(JobOrder.Customer));
            ModelState.Remove(nameof(JobOrder.Vehicle));
            ModelState.Remove(nameof(JobOrder.User));

            // Both are non-nullable, so an empty pick already fails binding -
            // but as "The value '' is invalid", which tells the person at the
            // counter nothing. Said plainly instead. A job order is work on a
            // particular car: without the vehicle there is no classification,
            // and without that nothing on it can be priced.
            if (jobOrder.CustomerID <= 0)
            {
                ModelState.Remove(nameof(JobOrder.CustomerID));
                ModelState.AddModelError(nameof(JobOrder.CustomerID), "Choose the customer.");
            }

            if (jobOrder.VehicleID <= 0)
            {
                ModelState.Remove(nameof(JobOrder.VehicleID));
                ModelState.AddModelError(nameof(JobOrder.VehicleID),
                    "Choose the vehicle. A job order is work on a particular car, and its class is what prices the work.");
            }

            // The picker will not offer a day that has gone, but that is the
            // browser's doing and a posted form need not have come from it.
            if (jobOrder.InstallationDate is { } installOn && installOn.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(JobOrder.InstallationDate),
                    "Installation can't be dated before today.");
            }

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(jobOrder);
                return View(jobOrder);
            }

            jobOrder.UserId = CurrentUserId;
            jobOrder.JobOrderDate = DateTime.Now;
            jobOrder.Status = "Pending";
            jobOrder.JobOrderNumber = "PENDING";

            _context.JobOrders.Add(jobOrder);
            await _context.SaveChangesAsync();

            // Number depends on the generated ID, so it's set in a second save.
            jobOrder.JobOrderNumber = DocumentNumber.For(
                DocumentNumber.JobOrder, jobOrder.JobOrderID, jobOrder.JobOrderDate);
            await _context.SaveChangesAsync();

            // Straight to the walkaround rather than to the job order.
            //
            // The condition of a car is a fact about the moment it arrived,
            // and the moment has to be now: an hour later the film is on and
            // nobody can say whether the scratch was there first. Leaving the
            // checklist as something to remember afterwards means it is
            // remembered only when there is a dispute, which is too late.
            //
            // The job order is one press away: the walkaround's Back goes to
            // it, and saving the checklist lands there too.
            TempData["Success"] =
                $"Job order {jobOrder.JobOrderNumber} raised. Walk the car before any film is touched.";

            return RedirectToAction("ForJobOrder", "VehicleChecklist", new
            {
                jobOrderId = jobOrder.JobOrderID,
                returnUrl = Url.Action(nameof(Details), new { id = jobOrder.JobOrderID })
            });
        }

        // GET: JobOrder/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var jobOrder = await _context.JobOrders.FindAsync(id);
            if (jobOrder == null)
                return NotFound();

            await LoadHeaderDropdowns(jobOrder);
            return View(jobOrder);
        }

        // POST: JobOrder/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("JobOrderID,CustomerID,VehicleID,JobTypeID,AssignedEmployeeID,InstallationDate,ExistingFilmShade,ReasonForChanging,SpecialInstruction,Complaint")]
            JobOrder jobOrder)
        {
            if (id != jobOrder.JobOrderID)
                return NotFound();

            ModelState.Remove(nameof(JobOrder.JobOrderNumber));
            ModelState.Remove(nameof(JobOrder.UserId));
            ModelState.Remove(nameof(JobOrder.Customer));
            ModelState.Remove(nameof(JobOrder.Vehicle));
            ModelState.Remove(nameof(JobOrder.User));

            // A job order installed last month keeps the date it was
            // installed on; what is refused is moving one INTO the past, so
            // correcting a complaint on an old order is not blocked by a date
            // nobody touched.
            var storedInstall = await _context.JobOrders
                .AsNoTracking()
                .Where(j => j.JobOrderID == id)
                .Select(j => j.InstallationDate)
                .FirstOrDefaultAsync();

            if (jobOrder.InstallationDate is { } movedTo
                && movedTo.Date < DateTime.Today
                && movedTo != storedInstall)
            {
                ModelState.AddModelError(nameof(JobOrder.InstallationDate),
                    "Installation can't be moved to a date before today.");
            }

            if (!ModelState.IsValid)
            {
                await LoadHeaderDropdowns(jobOrder);
                return View(jobOrder);
            }

            var existing = await _context.JobOrders.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.CustomerID = jobOrder.CustomerID;
            existing.VehicleID = jobOrder.VehicleID;
            existing.JobTypeID = jobOrder.JobTypeID;
            existing.AssignedEmployeeID = jobOrder.AssignedEmployeeID;
            existing.InstallationDate = jobOrder.InstallationDate;
            existing.ExistingFilmShade = jobOrder.ExistingFilmShade;
            existing.ReasonForChanging = jobOrder.ReasonForChanging;
            existing.SpecialInstruction = jobOrder.SpecialInstruction;
            existing.Complaint = jobOrder.Complaint;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: JobOrder/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var jobOrder = await _context.JobOrders
                .Include(j => j.Customer)
                .Include(j => j.Vehicle)
                .FirstOrDefaultAsync(j => j.JobOrderID == id);

            if (jobOrder == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(jobOrder);
        }

        // POST: JobOrder/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jobOrder = await _context.JobOrders.FindAsync(id);
            if (jobOrder == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.JobOrders.Remove(jobOrder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this job order. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: JobOrder/AddLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLine(
            int jobOrderId,
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
            var jobOrder = await _context.JobOrders
                .Include(j => j.Vehicle)
                .FirstOrDefaultAsync(j => j.JobOrderID == jobOrderId);

            if (jobOrder == null)
                return NotFound();

            // A completed job order has already taken its products out of
            // stock, and inv.InventoryTransaction has no link back to the line
            // that moved them, so a line added now could never be matched up.
            // Ask for the order to be reopened instead.
            if (jobOrder.Status == "Completed")
            {
                TempData["LineError"] =
                    "This job order is completed and its products have already left stock. " +
                    "Set it back to In Progress to change its line items.";
                return RedirectToAction(nameof(Details), new { id = jobOrderId });
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
                            p.VehicleClassificationID == jobOrder.Vehicle.VehicleClassificationID &&
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
                // a disabled box or posting the form by hand changes nothing.
                //
                // A combination the book has no price for is the one case a
                // staff member may still type: refusing it would mean a job
                // nobody can raise until an administrator is found.
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

                // A service is on the order once. Doing the fitting twice is
                // a quantity of two on the one line, not two lines - and
                // unlike a product, there is no second price for labour to
                // justify a separate one. Adding it again is a slip, so it is
                // said out loud rather than quietly folded into the line.
                if (serviceId != null)
                {
                    var already = await _context.JobOrderDetails
                        .Include(d => d.Service)
                        .FirstOrDefaultAsync(d =>
                            d.JobOrderID == jobOrderId && d.ServiceID == serviceId);

                    if (already != null)
                    {
                        TempData["LineError"] =
                            $"\"{already.Service?.ServiceName}\" is already on this job order "
                            + $"at a quantity of {already.Quantity:0.##}. "
                            + "Change that line's quantity rather than adding it again.";

                        return RedirectToAction(nameof(Details), new { id = jobOrderId });
                    }
                }

                var lineUnit = string.IsNullOrWhiteSpace(unit) ? "Unit" : unit;

                // A panel is what the line COVERS, not what it costs. Tying
                // it to pricingId meant a film with no price row for this
                // class of vehicle had its panel thrown away too, so the line
                // reached the job sheet naming the film and nothing else -
                // not which glass it was cut for. Keep any real panel and let
                // the price fend for itself, exactly as the variant and the
                // shade above.
                int? linePanelId = null;
                if (productId != null && panelId != null
                    && await _context.Panels.AnyAsync(p => p.PanelID == panelId))
                {
                    linePanelId = panelId;
                }

                // The same PRODUCT added twice is one line of two, not two
                // lines of one. Three rows reading "Armor Rear Side Step" at
                // 1, 1 and 4 tell a customer nothing the single row "6" does
                // not, and they invite somebody deleting a duplicate to
                // remove a quantity that was really sold. A service never
                // reaches here - it is refused above.
                //
                // Only a line that matches in every respect is added to:
                // same product or service, same variant, shade and panel,
                // same unit and the same price. A different price is a
                // different deal - a discounted second fitting is its own
                // line and has to stay one.
                var existing = await _context.JobOrderDetails
                    .FirstOrDefaultAsync(d =>
                        d.JobOrderID == jobOrderId &&
                        d.ProductID == productId &&
                        d.ServiceID == serviceId &&
                        d.TintVariantID == keptVariantId &&
                        d.ShadeID == keptShadeId &&
                        d.PanelID == linePanelId &&
                        d.Unit == lineUnit &&
                        d.UnitPrice == unitPrice);

                if (existing != null)
                {
                    existing.Quantity += quantity;

                    // A description typed on the second go is kept only where
                    // the first had none, so adding to a line cannot quietly
                    // rewrite what it says.
                    if (string.IsNullOrWhiteSpace(existing.Description)
                        && !string.IsNullOrWhiteSpace(description))
                        existing.Description = description;

                    TempData["Success"] =
                        $"Added {quantity} to the existing line - it now reads {existing.Quantity}.";
                }
                else
                {
                    _context.JobOrderDetails.Add(new JobOrderDetail
                    {
                        JobOrderID = jobOrderId,
                        ProductID = productId,
                        ServiceID = serviceId,
                        TintVariantID = keptVariantId,
                        ShadeID = keptShadeId,
                        PanelID = linePanelId,
                        PricingID = pricingId,
                        Description = description,
                        Quantity = quantity,
                        Unit = lineUnit,
                        UnitPrice = unitPrice
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = jobOrderId });
        }

        // POST: JobOrder/RemoveLine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLine(int detailId, int jobOrderId)
        {
            // Same reason as AddLine: the stock is already out.
            if (await _context.JobOrders.AnyAsync(j => j.JobOrderID == jobOrderId && j.Status == "Completed"))
            {
                TempData["LineError"] =
                    "This job order is completed and its products have already left stock. " +
                    "Set it back to In Progress to change its line items.";
                return RedirectToAction(nameof(Details), new { id = jobOrderId });
            }

            var detail = await _context.JobOrderDetails.FindAsync(detailId);
            if (detail != null)
            {
                _context.JobOrderDetails.Remove(detail);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = jobOrderId });
        }

        // POST: JobOrder/SetStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var allowed = new[] { "Pending", "In Progress", "Completed", "Cancelled" };
            if (!allowed.Contains(status))
                return BadRequest();

            var jobOrder = await _context.JobOrders.FindAsync(id);
            if (jobOrder == null)
                return RedirectToAction(nameof(Details), new { id });

            // Completing a job order is the moment its products are actually
            // fitted to the car, so that is where they leave stock. Moving it
            // back out of Completed puts them back, which keeps the two
            // transitions symmetrical: Completed -> Pending -> Completed nets
            // to a single OUT rather than two.
            var holdsStock = jobOrder.Status == "Completed";
            var willHoldStock = status == "Completed";

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (holdsStock != willHoldStock)
                    await _documentStock.Post(
                        DocumentKind.JobOrder, id, willHoldStock, CurrentUserId);

                jobOrder.Status = status;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (InvalidOperationException ex)
            {
                // Short on stock. Leave the job order as it was rather than
                // completing it against inventory that isn't there.
                await tx.RollbackAsync();
                TempData["StatusError"] = ex.Message;

                // The only stock problem that is fixed somewhere else, so the
                // banner carries a way to get there.
                if (ex is CutSizeMissingException)
                    TempData["StatusErrorFix"] = "CutSize";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // JobOrder is ON DELETE RESTRICT from ServiceInvoice, VehicleChecklist and Warranty.
        private async Task<string?> BuildBlockReason(int jobOrderId)
        {
            var invoices = await _context.ServiceInvoices.CountAsync(i => i.JobOrderID == jobOrderId);
            var checklists = await _context.VehicleChecklists.CountAsync(c => c.JobOrderID == jobOrderId);
            var warranties = await _context.Warranties.CountAsync(w => w.JobOrderID == jobOrderId);

            var parts = new List<string>();
            if (invoices > 0) { parts.Add($"{invoices} service invoice{(invoices == 1 ? "" : "s")}"); }
            if (checklists > 0) { parts.Add($"{checklists} vehicle checklist{(checklists == 1 ? "" : "s")}"); }
            if (warranties > 0) { parts.Add($"{warranties} warranty record{(warranties == 1 ? "" : "s")}"); }

            if (parts.Count == 0)
                return null;

            return "Can't delete this job order. It still has " + string.Join(", ", parts) +
                   " linked to it. Remove those first.";
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadHeaderDropdowns(JobOrder? jobOrder = null)
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "CustomerID", "FullName", jobOrder?.CustomerID);

            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.VehicleID,
                    Display = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model + " (" + v.Customer.FullName + ")"
                })
                .ToListAsync();
            ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "Display", jobOrder?.VehicleID);

            ViewData["JobTypeID"] = new SelectList(
                await _context.JobTypes.OrderBy(j => j.JobTypeName).ToListAsync(),
                "JobTypeID", "JobTypeName", jobOrder?.JobTypeID);

            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName)
                .Select(e => new { e.EmployeeID, Display = e.FirstName + " " + e.LastName })
                .ToListAsync();
            ViewData["AssignedEmployeeID"] = new SelectList(employees, "EmployeeID", "Display", jobOrder?.AssignedEmployeeID);
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

            // The shades each tint variant comes in, so the add-line form can
            // offer them once a variant is chosen. ops.JobOrderDetail has had
            // a ShadeID column all along and nothing ever filled it, so the
            // job sheet and the receipt could not say which shade was fitted -
            // and neither could a warranty claim months later.
            //
            // It does not change what comes off the shelf: inv.InventoryTransaction
            // records a product and nothing finer, so every shade of a film
            // draws on one balance. This is the record of what was fitted.
            // Named camelCase deliberately, the same as PricingMatrix above:
            // System.Text.Json keeps anonymous-type property names exactly as
            // written, so "ShadeID" would reach the browser as "ShadeID" and a
            // script reading shadeId would find nothing on every row.
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
                .OrderBy(s => s.ShadeName)
                .Select(s => new
                {
                    tintVariantId = s.TintVariantID,
                    shadeId = s.ShadeID,
                    shadeName = s.ShadeName
                })
                .ToListAsync();
        }
    }
}
