using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;
using ZEmpireAutoAccessories.Services;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// The condition of a car as it arrived, panel by panel, agreed with the
    /// customer before anyone touches it. Its whole worth is being written
    /// down first and being shown later, which is why the walkaround is one
    /// form and why there is a copy to hand over.
    /// </summary>
    [ModuleAuthorize("Vehicle Checklist")]
    public class VehicleChecklistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IChecklistPhotoStore _photos;

        public VehicleChecklistController(
            ApplicationDbContext context,
            IChecklistPhotoStore photos)
        {
            _context = context;
            _photos = photos;
        }

        public async Task<IActionResult> Index(string? q, int? page = null)
        {
            var query = _context.VehicleChecklists
                .Include(c => c.JobOrder).ThenInclude(j => j.Customer)
                .Include(c => c.JobOrder).ThenInclude(j => j.Vehicle)
                .Include(c => c.User)
                .Include(c => c.Details)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c =>
                    c.JobOrder.JobOrderNumber.Contains(term) ||
                    c.JobOrder.Customer.FullName.Contains(term) ||
                    (c.JobOrder.Vehicle.PlateNumber != null && c.JobOrder.Vehicle.PlateNumber.Contains(term)));
            }

            var checklists = await query
                .OrderByDescending(c => c.ChecklistDate)
                .ToPageAsync(page, ViewData);

            var panels = await _context.Panels.AsNoTracking().ToListAsync();

            ViewData["Findings"] = checklists.ToDictionary(
                c => c.ChecklistID,
                c => ChecklistFindings.For(
                    c.Details,
                    WalkaroundPanels.Walkaround(panels, c.Details.Select(d => d.PanelID)).Count));

            ViewData["Search"] = q;

            return View(checklists);
        }

        /// <summary>
        /// Where "Back to list" should land: the screen that sent the user
        /// here, when that is somewhere in this application, and the checklist
        /// list otherwise.
        ///
        /// A checklist is nearly always opened FROM a job order, and sending
        /// somebody to the checklist list afterwards strands them two screens
        /// from the job they were working on.
        /// </summary>
        /// <summary>
        /// Where saving lands: the screen this was opened from - a job order,
        /// nearly always - and the checklist itself when it was opened on its
        /// own.
        /// </summary>
        private IActionResult BackToOrChecklist(string? returnUrl, int checklistId) =>
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Details), new { id = checklistId });

        private string BackTo(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action(nameof(Index))!;

        public async Task<IActionResult> Details(int? id, string? returnUrl = null)
        {
            if (id == null)
                return NotFound();

            ViewData["BackUrl"] = BackTo(returnUrl);
            ViewData["ReturnUrl"] = returnUrl;

            var checklist = await LoadChecklist(id.Value);

            if (checklist == null)
                return NotFound();

            await LoadWalkaround(checklist);

            ViewData["Photos"] = _photos.List(checklist.ChecklistID);
            ViewData["MaxPhotos"] = _photos.MaxPerChecklist;
            ViewData["MaxPhotoMb"] = _photos.MaxBytes / 1024m / 1024m;

            return View(checklist);
        }

        // POST: VehicleChecklist/AddPhoto/5
        //
        // A separate form from the walkaround, because a form cannot hold
        // another one and the walkaround is one large form of its own.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(16 * 1024 * 1024)]
        public async Task<IActionResult> AddPhoto(int id, IFormFile? photo)
        {
            if (!await _context.VehicleChecklists.AnyAsync(c => c.ChecklistID == id))
                return NotFound();

            if (photo == null)
                TempData["PhotoError"] = "Choose a photo to attach.";
            else
                TempData["PhotoError"] = await _photos.Save(id, photo);

            if (TempData["PhotoError"] == null)
                TempData["Success"] = "Photo attached.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: VehicleChecklist/Photo/5?slot=2
        //
        // The photos are outside wwwroot, so they come back through here,
        // behind the module permission this controller already carries.
        public async Task<IActionResult> Photo(int id, int slot)
        {
            if (!await _context.VehicleChecklists.AnyAsync(c => c.ChecklistID == id))
                return NotFound();

            var stored = _photos.Open(id, slot);
            if (stored == null)
                return NotFound();

            var (content, contentType, fileName) = stored.Value;

            return File(content, contentType, fileName);
        }

        // POST: VehicleChecklist/RemovePhoto/5
        //
        // Named Remove rather than Delete on purpose. Deleting is the Admin's
        // alone across the system, and an action beginning "Delete" is held
        // to that automatically - but taking off a photo that came out blurred
        // is part of recording the walkaround, the same as RemoveLine is part
        // of writing a job order, and the person at the counter has to be able
        // to do it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto(int id, int slot)
        {
            if (!await _context.VehicleChecklists.AnyAsync(c => c.ChecklistID == id))
                return NotFound();

            if (_photos.Remove(id, slot))
                TempData["Success"] = "Photo removed.";

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Create(int? jobOrderId, string? returnUrl = null)
        {
            var checklist = new VehicleChecklist
            {
                ChecklistDate = DateTime.Now,
                JobOrderID = jobOrderId ?? 0
            };

            await LoadForm(checklist.JobOrderID);

            ViewData["MaxPhotos"] = _photos.MaxPerChecklist;
            ViewData["MaxPhotoMb"] = _photos.MaxBytes / 1024m / 1024m;

            return View(checklist);
        }

        // The header and the whole walkaround arrive together. Recording a
        // panel used to be its own page load - seven panels, seven round
        // trips through the server, on a phone, in a workshop.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Five photographs at fifteen megabytes apiece, plus the sheet.
        [RequestSizeLimit(80 * 1024 * 1024)]
        public async Task<IActionResult> Create(
            [Bind("JobOrderID,AdditionalNotes,ClientSignature")] VehicleChecklist checklist,
            List<int> panelId,
            List<string?> existingCondition,
            List<string?> notes,
            List<IFormFile>? photos = null,
            string? returnUrl = null)
        {
            ModelState.Remove(nameof(VehicleChecklist.UserId));
            ModelState.Remove(nameof(VehicleChecklist.JobOrder));
            ModelState.Remove(nameof(VehicleChecklist.User));

            var typed = Typed(panelId, existingCondition, notes);

            if (checklist.JobOrderID > 0)
            {
                var existing = await _context.VehicleChecklists
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.JobOrderID == checklist.JobOrderID);

                // One visit, one condition record. Two sheets for the same job
                // order is two answers to "how did the car arrive".
                if (existing != null)
                {
                    ModelState.AddModelError(nameof(VehicleChecklist.JobOrderID),
                        "That job order already has a checklist. Open the existing one to change it.");
                    ViewData["ExistingChecklistID"] = existing.ChecklistID;
                }
            }

            if (!ModelState.IsValid)
            {
                // Whoever typed this walked around the car to get it. Losing
                // it over a wrong job order would mean walking it again.
                await LoadForm(checklist.JobOrderID, typed);

                ViewData["MaxPhotos"] = _photos.MaxPerChecklist;
                ViewData["MaxPhotoMb"] = _photos.MaxBytes / 1024m / 1024m;

                return View(checklist);
            }

            checklist.UserId = CurrentUserId;
            checklist.ChecklistDate = DateTime.Now;

            foreach (var (id, row) in typed)
            {
                if (row.Condition == null && row.Notes == null)
                    continue;

                checklist.Details.Add(new VehicleChecklistDetail
                {
                    PanelID = id,
                    ExistingCondition = row.Condition,
                    Notes = row.Notes
                });
            }

            _context.VehicleChecklists.Add(checklist);
            await _context.SaveChangesAsync();

            // Now, and not before: a photograph is filed against a checklist,
            // and until SaveChanges there was no id to file it against.
            var stored = 0;
            var refusals = new List<string>();

            foreach (var photo in (photos ?? new List<IFormFile>()).Where(f => f is { Length: > 0 }))
            {
                var refused = await _photos.Save(checklist.ChecklistID, photo);

                if (refused == null)
                    stored++;
                else
                    refusals.Add($"{photo.FileName}: {refused}");
            }

            // A refused photo does not undo the walkaround. Somebody stood at
            // the car to type it, and losing that over a file that was too
            // big would be the worse trade - so the sheet is saved and the
            // refusal is reported against it.
            if (refusals.Count > 0)
                TempData["PhotoError"] = string.Join(" ", refusals);

            var panels = checklist.Details.Count == 0
                ? "No panel conditions recorded yet."
                : $"{checklist.Details.Count} panel{(checklist.Details.Count == 1 ? "" : "s")} recorded.";

            TempData["Success"] = stored == 0
                ? $"Checklist saved. {panels}"
                : $"Checklist saved with {stored} photo{(stored == 1 ? "" : "s")}. {panels}";

            // Back to the job order this was raised from. The walkaround is a
            // step in writing one up, not a place to be left standing - the
            // checklist is one press away from the job order whenever it is
            // wanted again.
            return BackToOrChecklist(returnUrl, checklist.ChecklistID);
        }

        /// <summary>
        /// Saves the whole sheet - the panels and the two header fields that
        /// cannot be known at intake. The customer signs after seeing the
        /// walkaround, so the signature has to be editable afterwards; before
        /// this it could only ever be set on the create form and never again.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDetails(
            int id,
            string? additionalNotes,
            string? clientSignature,
            List<int> panelId,
            List<string?> existingCondition,
            List<string?> notes,
            string? returnUrl = null)
        {
            var checklist = await _context.VehicleChecklists
                .Include(c => c.Details)
                .FirstOrDefaultAsync(c => c.ChecklistID == id);

            if (checklist == null)
                return NotFound();

            var header = Clean(additionalNotes);
            var signature = Clean(clientSignature);

            // Nothing binds the entity here, so the one length the model
            // declares has to be checked by hand or SaveChanges throws on
            // truncation. ClientSignature declares none, so none is invented.
            if (header?.Length > 400)
            {
                TempData["SaveError"] = "Additional notes are limited to 400 characters.";
                return RedirectToAction(nameof(Details), new { id });
            }

            checklist.AdditionalNotes = header;
            checklist.ClientSignature = signature;

            var saved = 0;

            foreach (var (panel, row) in Typed(panelId, existingCondition, notes))
            {
                var detail = checklist.Details.FirstOrDefault(d => d.PanelID == panel);

                if (row.Condition == null && row.Notes == null)
                {
                    // A panel cleared back to empty should not leave a row
                    // behind: "never looked at" and "looked at, said nothing"
                    // have to stay different answers.
                    if (detail != null)
                        _context.VehicleChecklistDetails.Remove(detail);

                    continue;
                }

                if (detail == null)
                {
                    checklist.Details.Add(new VehicleChecklistDetail
                    {
                        ChecklistID = id,
                        PanelID = panel,
                        ExistingCondition = row.Condition,
                        Notes = row.Notes
                    });
                }
                else
                {
                    detail.ExistingCondition = row.Condition;
                    detail.Notes = row.Notes;
                }

                saved++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Saved. {saved} panel{(saved == 1 ? "" : "s")} recorded on this checklist.";

            return RedirectToAction(nameof(Details), new { id, returnUrl });
        }

        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var checklist = await LoadChecklist(id.Value);

            if (checklist == null)
                return NotFound();

            var panels = await _context.Panels.AsNoTracking().ToListAsync();
            var walkaround = WalkaroundPanels.Walkaround(panels, checklist.Details.Select(d => d.PanelID));

            var pdf = DocumentPdfBuilder.BuildVehicleChecklistPdf(checklist, walkaround);

            return File(pdf, "application/pdf",
                $"Checklist-{checklist.JobOrder.JobOrderNumber}.pdf");
        }

        /// <summary>
        /// The way in from a job order: open its checklist if it has one,
        /// otherwise start one for it. The job order screen cannot know
        /// which, and should not have to.
        /// </summary>
        public async Task<IActionResult> ForJobOrder(int jobOrderId, string? returnUrl = null)
        {
            var existing = await _context.VehicleChecklists
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.JobOrderID == jobOrderId);

            return existing == null
                ? RedirectToAction(nameof(Create), new { jobOrderId, returnUrl })
                : RedirectToAction(nameof(Details), new { id = existing.ChecklistID, returnUrl });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var checklist = await _context.VehicleChecklists
                .Include(c => c.JobOrder).ThenInclude(j => j.Customer)
                .Include(c => c.JobOrder).ThenInclude(j => j.Vehicle)
                .Include(c => c.Details)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChecklistID == id);

            if (checklist == null)
                return NotFound();

            return View(checklist);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var checklist = await _context.VehicleChecklists.FindAsync(id);

            if (checklist != null)
            {
                _context.VehicleChecklists.Remove(checklist);
                await _context.SaveChangesAsync();

                // The photos are not rows, so nothing cascades to them. Left
                // behind they would outlive the checklist for good, and a new
                // checklist reusing the id would inherit them.
                _photos.RemoveAll(id);

                TempData["Success"] = "Checklist deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private Task<VehicleChecklist?> LoadChecklist(int id) =>
            _context.VehicleChecklists
                .Include(c => c.JobOrder).ThenInclude(j => j.Customer)
                .Include(c => c.JobOrder).ThenInclude(j => j.Vehicle)
                .Include(c => c.JobOrder).ThenInclude(j => j.JobType)
                .Include(c => c.User)
                .Include(c => c.Details).ThenInclude(d => d.Panel)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChecklistID == id);

        private async Task LoadWalkaround(VehicleChecklist checklist)
        {
            var panels = await _context.Panels.AsNoTracking().ToListAsync();
            var walkaround = WalkaroundPanels.Walkaround(panels, checklist.Details.Select(d => d.PanelID));

            ViewData["Rows"] = WalkaroundPanels.Rows(walkaround, checklist.Details);
            ViewData["Findings"] = ChecklistFindings.For(checklist.Details, walkaround.Count);
        }

        private async Task LoadForm(
            int selectedJobOrderId,
            IReadOnlyDictionary<int, (string? Condition, string? Notes)>? typed = null)
        {
            // A job order that already has a sheet is not offered - that is
            // the duplicate caught before anyone types a walkaround into it.
            // The one being re-rendered after a validation failure stays on
            // the list so the form can show what was picked.
            var jobOrders = await _context.JobOrders
                .Where(j => j.JobOrderID == selectedJobOrderId
                            || !_context.VehicleChecklists.Any(c => c.JobOrderID == j.JobOrderID))
                .OrderByDescending(j => j.JobOrderDate)
                .Select(j => new
                {
                    j.JobOrderID,
                    j.JobOrderNumber,
                    j.JobOrderDate,
                    Customer = (string?)j.Customer.FullName,
                    Plate = j.Vehicle.PlateNumber
                })
                .AsNoTracking()
                .ToListAsync();

            // Formatted here rather than in the query: SQL Server has no
            // "dd MMM yyyy", and its + turns the whole label into NULL if a
            // customer row happens to have no name.
            var options = jobOrders
                .Select(j => new
                {
                    j.JobOrderID,
                    Display = $"{j.JobOrderNumber} — {j.Customer ?? "No name"} "
                              + $"({(string.IsNullOrWhiteSpace(j.Plate) ? "No plate" : j.Plate)}) · "
                              + j.JobOrderDate.ToString("dd MMM yyyy")
                })
                .ToList();

            ViewData["JobOrderID"] = new SelectList(options, "JobOrderID", "Display", selectedJobOrderId);
            ViewData["JobOrdersAvailable"] = options.Count;
            ViewData["JobOrdersTotal"] = await _context.JobOrders.CountAsync();

            var panels = await _context.Panels.AsNoTracking().ToListAsync();
            var walkaround = WalkaroundPanels.Walkaround(panels, Array.Empty<int>());

            ViewData["Rows"] = WalkaroundPanels.Rows(
                walkaround, Array.Empty<VehicleChecklistDetail>(), typed);
        }

        /// <summary>
        /// The posted sheet, keyed by panel, with blanks turned into nulls so
        /// a whitespace-only box is not stored as a condition.
        /// </summary>
        private static Dictionary<int, (string? Condition, string? Notes)> Typed(
            List<int> panelId, List<string?> existingCondition, List<string?> notes)
        {
            var rows = new Dictionary<int, (string? Condition, string? Notes)>();

            for (var i = 0; i < panelId.Count; i++)
            {
                rows[panelId[i]] = (
                    Clean(i < existingCondition.Count ? existingCondition[i] : null),
                    Clean(i < notes.Count ? notes[i] : null));
            }

            return rows;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
