using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// Cashiering is a read-only collections view: it merges product sales
    /// (sales.Sales) and service invoices (sales.ServiceInvoice) into one
    /// list so a cashier can close out a day. Recording and editing either
    /// document stays in SalesController / ServiceInvoiceController; the rows
    /// here link back to those screens.
    ///
    /// It deliberately has no module row of its own in sec.Module: it shows
    /// nothing that Sales and Service Invoices do not already own, so it is
    /// gated on those two and needs no database change to switch on. Holding
    /// either one opens the screen, and the user sees only the half they
    /// hold - a Sales-only user gets sales, and never service invoice rows.
    /// </summary>
    [ModuleAuthorize("Sales", "Service Invoices")]
    public class CashieringController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICashieringService _cashieringService;
        private readonly IProofStore _proofs;

        public CashieringController(
            ApplicationDbContext context,
            ICashieringService cashieringService,
            IProofStore proofs)
        {
            _context = context;
            _cashieringService = cashieringService;
            _proofs = proofs;
        }

        // GET: Cashiering?from=...&to=...&q=...&paymentModeId=...&userId=...&source=Sale
        public async Task<IActionResult> Index(
            DateOnly? from,
            DateOnly? to,
            string? q,
            int? paymentModeId,
            string? userId,
            CashierSource? source)
        {
            // A cashier opens this to close out the current shift, so default
            // to today rather than to the whole table.
            var today = DateOnly.FromDateTime(DateTime.Now);
            var dateFrom = from ?? today;
            var dateTo = to ?? today;

            // The filter says what was asked for; the claims say what may be
            // answered. Someone holding only one of the two modules is pinned
            // to that source, whatever arrives on the query string - the
            // ModuleAuthorize above lets either module in, so this is what
            // keeps the other half out.
            var canSeeSales = User.HasClaim(AppClaims.ModuleAccess, "Sales");
            var canSeeInvoices = User.HasClaim(AppClaims.ModuleAccess, "Service Invoices");

            var effectiveSource = source;
            if (!canSeeSales)
                effectiveSource = CashierSource.ServiceInvoice;
            else if (!canSeeInvoices)
                effectiveSource = CashierSource.Sale;

            var model = await _cashieringService.GetCashiering(
                dateFrom, dateTo, q, paymentModeId, userId, effectiveSource);

            model.CanSeeSales = canSeeSales;
            model.CanSeeServiceInvoices = canSeeInvoices;

            // The day's takings are Admin-only, the same way the dashboard
            // withholds peso figures from Staff. Cleared here rather than
            // merely hidden in the view, so a Staff response never carries
            // the numbers at all.
            model.CanSeeTotals = User.IsInRole("Admin");
            if (!model.CanSeeTotals)
            {
                model.SalesTotal = 0m;
                model.ServiceInvoiceTotal = 0m;
                model.GrandTotal = 0m;
                model.UncollectedTotal = 0m;
                model.CollectedCount = 0;
                model.UncollectedCount = 0;
                model.ByPaymentMode.Clear();
                model.ByCashier.Clear();
            }

            // Which rows already have a receipt attached. One folder listing
            // for the whole page - see PaymentProofStore.
            var attached = _proofs.ExistingFor(
                model.Transactions.Select(t => (ProofKinds.For(t.Source), t.SourceId)));

            foreach (var transaction in model.Transactions)
                transaction.HasProof =
                    attached.Contains((ProofKinds.For(transaction.Source), transaction.SourceId));

            await LoadFilterDropdowns(model);

            return View(model);
        }

        // POST: Cashiering/AttachProof
        //
        // The screenshot or receipt for a payment that arrived from somewhere
        // other than the counter. Stored against the transaction and nowhere
        // else; the file itself never becomes part of the sale record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> AttachProof(
            CashierSource source, int sourceId, IFormFile? proof, string? returnUrl)
        {
            if (!MaySee(source))
                return Forbid();

            if (!await TransactionExists(source, sourceId))
                return NotFound();

            if (proof == null)
                TempData["ProofError"] = "Choose a file to attach.";
            else
                TempData["ProofError"] = await _proofs.Save(ProofKinds.For(source), sourceId, proof);

            if (TempData["ProofError"] == null)
                TempData["Success"] = "Proof of payment attached.";

            return RedirectBack(returnUrl);
        }

        // GET: Cashiering/Proof?source=Sale&sourceId=5
        //
        // Streams a stored receipt. These carry names, reference numbers and
        // account details, which is why the folder sits outside wwwroot and
        // why this is the only way to read one.
        public IActionResult Proof(CashierSource source, int sourceId)
        {
            if (!MaySee(source))
                return Forbid();

            var stored = _proofs.Open(ProofKinds.For(source), sourceId);
            if (stored == null)
                return NotFound();

            var (content, contentType, fileName) = stored.Value;

            // Shown in the browser rather than downloaded - a cashier is
            // checking a screenshot, not collecting files. The content type
            // comes from the store's own allow-list, never from the upload.
            return File(content, contentType, fileName, enableRangeProcessing: false);
        }

        // POST: Cashiering/RemoveProof
        //
        // For the wrong file - the customer's other receipt, the screenshot
        // before it finished sending. The row goes back to asking for one, so
        // a removal cannot quietly leave a payment looking settled.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveProof(CashierSource source, int sourceId, string? returnUrl)
        {
            if (!MaySee(source))
                return Forbid();

            if (_proofs.Delete(ProofKinds.For(source), sourceId))
                TempData["Success"] = "Proof of payment removed. Attach the right file in its place.";
            else
                TempData["ProofError"] = "There was nothing attached to remove.";

            return RedirectBack(returnUrl);
        }

        /// <summary>
        /// Whether this user holds the module the transaction belongs to. The
        /// class-level attribute lets in anyone holding either one, so a
        /// Sales-only user must not reach an invoice's receipt through it.
        /// </summary>
        private bool MaySee(CashierSource source) =>
            source == CashierSource.Sale
                ? User.HasClaim(AppClaims.ModuleAccess, "Sales")
                : User.HasClaim(AppClaims.ModuleAccess, "Service Invoices");

        private async Task<bool> TransactionExists(CashierSource source, int sourceId) =>
            source == CashierSource.Sale
                ? await _context.Sales.AnyAsync(s => s.SalesID == sourceId)
                : await _context.ServiceInvoices.AnyAsync(i => i.ServiceInvoiceID == sourceId);

        /// <summary>
        /// Back to the filtered list the upload came from, keeping the dates
        /// and filters that were set. Only a local path is followed - an
        /// absolute URL on the query string would make this an open redirect.
        /// </summary>
        private IActionResult RedirectBack(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));

        private async Task LoadFilterDropdowns(CashieringViewModel model)
        {
            ViewData["PaymentModeID"] = new SelectList(
                await _context.PaymentModes
                    .OrderBy(p => p.PaymentModeName)
                    .ToListAsync(),
                "PaymentModeID", "PaymentModeName", model.PaymentModeID);

            // Only users who have actually recorded a sale or an invoice -
            // listing every account would bury the handful of real cashiers.
            // Drawn from the same sources the user is allowed to see, so the
            // filter never names someone who only appears on the other half.
            IQueryable<string>? cashierIdQuery = null;

            if (model.CanSeeSales)
                cashierIdQuery = _context.Sales.Select(s => s.UserId);

            if (model.CanSeeServiceInvoices)
            {
                var invoiceUsers = _context.ServiceInvoices.Select(i => i.UserId);
                cashierIdQuery = cashierIdQuery == null
                    ? invoiceUsers
                    : cashierIdQuery.Union(invoiceUsers);
            }

            var cashierIds = cashierIdQuery == null
                ? new List<string>()
                : await cashierIdQuery.ToListAsync();

            var cashiers = await _context.Users
                .Where(u => cashierIds.Contains(u.Id))
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    Display = u.FullName != null && u.FullName != "" ? u.FullName : u.UserName
                })
                .ToListAsync();

            ViewData["UserId"] = new SelectList(cashiers, "Id", "Display", model.UserId);
        }
    }
}
