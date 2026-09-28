using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Warranty")]
    public class WarrantyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProofStore _proofs;

        private static readonly string[] StatusOptions = { "Active", "Expired", "Voided", "Claimed" };

        public WarrantyController(ApplicationDbContext context, IProofStore proofs)
        {
            _context = context;
            _proofs = proofs;
        }

        // GET: Warranty?status=Active&q=...
        public async Task<IActionResult> Index(string? status, string? q)
        {
            await AutoExpireOverdueWarranties();

            var query = _context.Warranties
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Sale)
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Product)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.ServiceInvoice)
                .Include(w => w.JobOrder).ThenInclude(j => j!.Customer)
                .Where(w => status == null || w.WarrantyStatus == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(w =>
                    (w.SalesDetail != null &&
                        (w.SalesDetail.Sale.InvoiceNumber.Contains(term) || w.SalesDetail.Product.ProductName.Contains(term))) ||
                    (w.ServiceInvoiceDetail != null && w.ServiceInvoiceDetail.ServiceInvoice.InvoiceNumber.Contains(term)) ||
                    (w.JobOrder != null &&
                        (w.JobOrder.JobOrderNumber.Contains(term) || w.JobOrder.Customer.FullName.Contains(term))));
            }

            var warranties = await query
                .OrderByDescending(w => w.WarrantyID)
                .ToListAsync();

            ViewData["Status"] = status;
            ViewData["Search"] = q;
            ViewData["Success"] = TempData["Success"];
            ViewData["ClaimError"] = TempData["ClaimError"];
            return View(warranties);
        }

        // GET: Warranty/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var warranty = await _context.Warranties
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Sale)
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Product)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.ServiceInvoice)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.Product)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.Service)
                .Include(w => w.JobOrder).ThenInclude(j => j!.Customer)
                .FirstOrDefaultAsync(w => w.WarrantyID == id);

            if (warranty == null)
                return NotFound();

            if (IsOverdue(warranty))
            {
                warranty.WarrantyStatus = "Expired";
                await _context.SaveChangesAsync();
            }

            ViewData["Success"] = TempData["Success"];
            ViewData["ClaimError"] = TempData["ClaimError"];
            ViewData["HasClaimProof"] = _proofs.Exists(ProofKind.WarrantyClaim, warranty.WarrantyID);
            return View(warranty);
        }

        // POST: Warranty/AttachClaimProof/5
        //
        // What shows the claim actually happened: the signed slip, the
        // replacement receipt, a photo of the work redone. Kept against the
        // warranty rather than in it - sales.Warranty has no column for a
        // file, and none is being added. See ProofStore.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> AttachClaimProof(int id, IFormFile? proof)
        {
            if (!await _context.Warranties.AnyAsync(w => w.WarrantyID == id))
                return NotFound();

            TempData["ClaimError"] = proof == null
                ? "Choose a file to attach."
                : await _proofs.Save(ProofKind.WarrantyClaim, id, proof);

            if (TempData["ClaimError"] == null)
                TempData["Success"] = "Proof of claim attached.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Warranty/ClaimProof/5
        //
        // Streamed rather than served as a static file: these carry customers'
        // names and signatures, and the folder is outside wwwroot for that
        // reason. Behind the Warranty module, like the rest of this screen.
        public IActionResult ClaimProof(int id)
        {
            var stored = _proofs.Open(ProofKind.WarrantyClaim, id);
            if (stored == null)
                return NotFound();

            var (content, contentType, fileName) = stored.Value;
            // No file name on purpose: passing one sets Content-Disposition
            // to attachment, which downloads the receipt instead of showing
            // it, and scatters customers' bank details through Downloads.
            return File(content, contentType);
        }

        // POST: Warranty/RemoveClaimProof/5
        //
        // For the wrong file. The claim itself stands - the status and the
        // remarks record that - so this only clears the attachment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveClaimProof(int id)
        {
            if (_proofs.Delete(ProofKind.WarrantyClaim, id))
                TempData["Success"] = "Proof of claim removed. Attach the right file in its place.";
            else
                TempData["ClaimError"] = "There was nothing attached to remove.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Warranty/Claim/5
        public async Task<IActionResult> Claim(int? id)
        {
            if (id == null)
                return NotFound();

            var warranty = await _context.Warranties
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Sale)
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Product)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.ServiceInvoice)
                .Include(w => w.JobOrder).ThenInclude(j => j!.Customer)
                .FirstOrDefaultAsync(w => w.WarrantyID == id);

            if (warranty == null)
                return NotFound();

            if (IsOverdue(warranty))
            {
                warranty.WarrantyStatus = "Expired";
                await _context.SaveChangesAsync();
            }

            if (warranty.WarrantyStatus != "Active")
            {
                TempData["ClaimError"] = warranty.WarrantyStatus == "Claimed"
                    ? "This warranty has already been claimed."
                    : $"This warranty can't be claimed - its status is {warranty.WarrantyStatus}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(warranty);
        }

        // POST: Warranty/Claim/5
        [HttpPost, ActionName("Claim")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimConfirmed(int id, string? claimNotes, IFormFile? claimProof)
        {
            var warranty = await _context.Warranties.FindAsync(id);
            if (warranty == null)
                return NotFound();

            if (IsOverdue(warranty))
                warranty.WarrantyStatus = "Expired";

            if (warranty.WarrantyStatus != "Active")
            {
                await _context.SaveChangesAsync();
                TempData["ClaimError"] = warranty.WarrantyStatus == "Claimed"
                    ? "This warranty has already been claimed."
                    : $"This warranty can't be claimed - its status is {warranty.WarrantyStatus}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var claimedBy = User.FindFirst(AppClaims.FullName)?.Value ?? User.Identity?.Name ?? "Unknown";
            var stamp = $"Claimed {DateTime.Now:MMM d, yyyy} by {claimedBy}" +
                        (string.IsNullOrWhiteSpace(claimNotes) ? "" : $": {claimNotes.Trim()}");

            // Remarks is limited to 150 chars in the DB - keep the newest note
            // and trim rather than risk a save failure on a long claim note.
            var combined = string.IsNullOrWhiteSpace(warranty.Remarks)
                ? stamp
                : warranty.Remarks + " | " + stamp;
            warranty.Remarks = combined.Length > 150 ? combined[..150] : combined;

            warranty.WarrantyStatus = "Claimed";
            await _context.SaveChangesAsync();

            // The proof is optional here - a claim that is genuinely recorded
            // should not be blocked because the customer has not sent the slip
            // yet. It can be attached from Details afterwards. A file that is
            // refused says so without undoing the claim.
            var proofError = claimProof == null
                ? null
                : await _proofs.Save(ProofKind.WarrantyClaim, id, claimProof);

            TempData["Success"] = proofError == null
                ? "Warranty claimed successfully."
                : "Warranty claimed successfully, but the file was not attached.";

            if (proofError != null)
                TempData["ClaimError"] = proofError;

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Warranty/Create?salesDetailId=5
        //      Warranty/Create?serviceInvoiceDetailId=7
        //      Warranty/Create?jobOrderId=3
        //
        // The Sale and Service Invoice detail screens link here per line item,
        // so the line is already known - pre-select it rather than making
        // someone find it again in a dropdown of every line ever recorded.
        public async Task<IActionResult> Create(int? salesDetailId, int? serviceInvoiceDetailId, int? jobOrderId)
        {
            var warranty = new Warranty
            {
                WarrantyStatus = "Active",
                SalesDetailID = salesDetailId,
                ServiceInvoiceDetailID = serviceInvoiceDetailId,
                JobOrderID = jobOrderId
            };

            // Arriving from a line item means the warranty starts today unless
            // the user says otherwise; a blank form leaves the dates alone.
            if (salesDetailId != null || serviceInvoiceDetailId != null || jobOrderId != null)
                warranty.WarrantyStartDate = DateOnly.FromDateTime(DateTime.Today);

            await DescribeLink(warranty);
            await LoadDropdowns(warranty);
            return View(warranty);
        }

        /// <summary>
        /// Describes the record this warranty is being linked to, for the
        /// Create form's context banner. Someone arriving from a sale line
        /// already knows what they are covering - the form should say it back
        /// rather than making them read it out of three dropdowns. Sets
        /// nothing when the form was opened blank, and the pickers show
        /// instead.
        /// </summary>
        private async Task DescribeLink(Warranty warranty)
        {
            if (warranty.SalesDetailID != null)
            {
                var line = await _context.SalesDetails
                    .Include(d => d.Sale).ThenInclude(s => s.Customer)
                    .Include(d => d.Product)
                    .FirstOrDefaultAsync(d => d.SalesDetailID == warranty.SalesDetailID);

                if (line != null)
                {
                    ViewData["LinkKind"] = "Sale";
                    ViewData["LinkRef"] = line.Sale.InvoiceNumber;
                    ViewData["LinkItem"] = $"{line.Product.ProductName} (Qty {line.Quantity})";
                    ViewData["LinkCustomer"] = line.Sale.Customer.FullName;
                    ViewData["LinkBackUrl"] = Url.Action("Details", "Sales", new { id = line.SalesID });
                }
                return;
            }

            if (warranty.ServiceInvoiceDetailID != null)
            {
                var line = await _context.ServiceInvoiceDetails
                    .Include(d => d.ServiceInvoice).ThenInclude(i => i.Customer)
                    .Include(d => d.Product)
                    .Include(d => d.Service)
                    .FirstOrDefaultAsync(d => d.ServiceInvoiceDetailID == warranty.ServiceInvoiceDetailID);

                if (line != null)
                {
                    ViewData["LinkKind"] = "Service Invoice";
                    ViewData["LinkRef"] = line.ServiceInvoice.InvoiceNumber;
                    ViewData["LinkItem"] =
                        line.Product?.ProductName ?? line.Service?.ServiceName ?? line.Description;
                    ViewData["LinkCustomer"] = line.ServiceInvoice.Customer.FullName;
                    ViewData["LinkBackUrl"] = Url.Action("Details", "ServiceInvoice", new { id = line.ServiceInvoiceID });
                }
                return;
            }

            if (warranty.JobOrderID != null)
            {
                var job = await _context.JobOrders
                    .Include(j => j.Customer)
                    .FirstOrDefaultAsync(j => j.JobOrderID == warranty.JobOrderID);

                if (job != null)
                {
                    ViewData["LinkKind"] = "Job Order";
                    ViewData["LinkRef"] = job.JobOrderNumber;
                    ViewData["LinkItem"] = job.Status;
                    ViewData["LinkCustomer"] = job.Customer.FullName;
                    ViewData["LinkBackUrl"] = Url.Action("Details", "JobOrder", new { id = job.JobOrderID });
                }
            }
        }

        // POST: Warranty/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("SalesDetailID,ServiceInvoiceDetailID,JobOrderID,WarrantyStartDate,WarrantyEndDate,WarrantyTerms,WarrantyStatus,Remarks")]
            Warranty warranty,
            bool linked = false)
        {
            if (warranty.SalesDetailID == null && warranty.ServiceInvoiceDetailID == null && warranty.JobOrderID == null)
            {
                ModelState.AddModelError(string.Empty, "Link the warranty to a sale item, a service invoice item, or a job order.");
            }

            if (warranty.WarrantyEndDate != null && warranty.WarrantyStartDate != null &&
                warranty.WarrantyEndDate < warranty.WarrantyStartDate)
            {
                ModelState.AddModelError(nameof(Warranty.WarrantyEndDate),
                    "The end date can't be before the start date.");
            }

            if (!ModelState.IsValid)
            {
                // Came in from a line item - keep the banner and the hidden
                // link across the round trip, rather than dropping the user
                // back to three empty pickers with no idea what they were
                // covering.
                if (linked)
                    await DescribeLink(warranty);

                await LoadDropdowns(warranty);
                return View(warranty);
            }

            warranty.CreatedBy = User.FindFirst(AppClaims.FullName)?.Value ?? User.Identity?.Name;

            _context.Warranties.Add(warranty);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = warranty.WarrantyID });
        }

        // GET: Warranty/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var warranty = await _context.Warranties.FindAsync(id);
            if (warranty == null)
                return NotFound();

            await LoadDropdowns(warranty);
            return View(warranty);
        }

        // POST: Warranty/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("WarrantyID,SalesDetailID,ServiceInvoiceDetailID,JobOrderID,WarrantyStartDate,WarrantyEndDate,WarrantyTerms,WarrantyStatus,Remarks")]
            Warranty warranty)
        {
            if (id != warranty.WarrantyID)
                return NotFound();

            if (warranty.SalesDetailID == null && warranty.ServiceInvoiceDetailID == null && warranty.JobOrderID == null)
            {
                ModelState.AddModelError(string.Empty, "Link the warranty to a sale item, a service invoice item, or a job order.");
            }

            // Same check Create makes - a cover that ends before it starts is
            // not a cover, and nothing in the database stops it.
            if (warranty.WarrantyEndDate != null && warranty.WarrantyStartDate != null &&
                warranty.WarrantyEndDate < warranty.WarrantyStartDate)
            {
                ModelState.AddModelError(nameof(Warranty.WarrantyEndDate),
                    "The end date can't be before the start date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(warranty);
                return View(warranty);
            }

            // Expired is itself an auto-managed state (see IsOverdue /
            // AutoExpireOverdueWarranties) - if the end date is pushed back
            // out and it's still sitting at Expired, it's stale. Voided and
            // Claimed are deliberate, so those are left alone.
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (warranty.WarrantyStatus == "Expired" &&
                (warranty.WarrantyEndDate == null || warranty.WarrantyEndDate >= today))
            {
                warranty.WarrantyStatus = "Active";
            }

            try
            {
                _context.Update(warranty);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Warranties.AnyAsync(w => w.WarrantyID == id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Warranty/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var warranty = await _context.Warranties
                .Include(w => w.SalesDetail).ThenInclude(d => d!.Sale)
                .Include(w => w.ServiceInvoiceDetail).ThenInclude(d => d!.ServiceInvoice)
                .Include(w => w.JobOrder)
                .FirstOrDefaultAsync(w => w.WarrantyID == id);

            if (warranty == null)
                return NotFound();

            return View(warranty);
        }

        // POST: Warranty/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var warranty = await _context.Warranties.FindAsync(id);
            if (warranty == null)
                return RedirectToAction(nameof(Index));

            try
            {
                _context.Warranties.Remove(warranty);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this warranty. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private static bool IsOverdue(Warranty warranty) =>
            warranty.WarrantyStatus == "Active" &&
            warranty.WarrantyEndDate.HasValue &&
            warranty.WarrantyEndDate.Value < DateOnly.FromDateTime(DateTime.Today);

        // Flip any Active warranty whose end date has already passed to Expired.
        // Runs on every Index load so the list always reflects today's date
        // without needing a background job.
        private async Task AutoExpireOverdueWarranties()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var overdue = await _context.Warranties
                .Where(w => w.WarrantyStatus == "Active" && w.WarrantyEndDate != null && w.WarrantyEndDate < today)
                .ToListAsync();

            if (overdue.Count == 0)
                return;

            foreach (var warranty in overdue)
                warranty.WarrantyStatus = "Expired";

            await _context.SaveChangesAsync();
        }

        private async Task LoadDropdowns(Warranty? warranty = null)
        {
            ViewData["StatusOptions"] = StatusOptions;

            var saleDetails = await _context.SalesDetails
                .Include(d => d.Sale)
                .Include(d => d.Product)
                .OrderByDescending(d => d.SalesDetailID)
                .Select(d => new
                {
                    d.SalesDetailID,
                    Display = d.Sale.InvoiceNumber + " — " + d.Product.ProductName + " (Qty " + d.Quantity + ")"
                })
                .ToListAsync();
            ViewData["SalesDetailID"] = new SelectList(saleDetails, "SalesDetailID", "Display", warranty?.SalesDetailID);

            var invoiceDetails = await _context.ServiceInvoiceDetails
                .Include(d => d.ServiceInvoice)
                .OrderByDescending(d => d.ServiceInvoiceDetailID)
                .Select(d => new
                {
                    d.ServiceInvoiceDetailID,
                    Display = d.ServiceInvoice.InvoiceNumber + " — " + d.Description
                })
                .ToListAsync();
            ViewData["ServiceInvoiceDetailID"] = new SelectList(invoiceDetails, "ServiceInvoiceDetailID", "Display", warranty?.ServiceInvoiceDetailID);

            var jobOrders = await _context.JobOrders
                .Include(j => j.Customer)
                .OrderByDescending(j => j.JobOrderID)
                .Select(j => new
                {
                    j.JobOrderID,
                    Display = j.JobOrderNumber + " — " + j.Customer.FullName
                })
                .ToListAsync();
            ViewData["JobOrderID"] = new SelectList(jobOrders, "JobOrderID", "Display", warranty?.JobOrderID);
        }
    }
}
