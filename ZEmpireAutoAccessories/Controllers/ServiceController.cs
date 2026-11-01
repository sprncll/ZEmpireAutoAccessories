using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Services")]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Service
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.ServiceCategory)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();

            return View(services);
        }

        // GET: Service/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefaultAsync(s => s.ServiceID == id);

            if (service == null)
                return NotFound();

            return View(service);
        }

        // GET: Service/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Service { IsActive = true });
        }

        // POST: Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ServiceCategoryID,ServiceName,Description,DefaultPrice,IsActive")] Service service)
        {
            ModelState.Remove(nameof(Service.ServiceCategory));

            // The form renders this read-only for staff, but a read-only field
            // still posts and a form can be replayed, so the value is dropped
            // here rather than trusted.
            // cat.Service.DefaultPrice is not nullable, so a service created by
            // staff starts at zero for an administrator to set, rather than at
            // whatever was typed.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                service.DefaultPrice = 0m;
                ModelState.Remove(nameof(Service.DefaultPrice));
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(service);
                return View(service);
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return NotFound();

            await LoadDropdowns(service);
            return View(service);
        }

        // POST: Service/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ServiceID,ServiceCategoryID,ServiceName,Description,DefaultPrice,IsActive")] Service service)
        {
            if (id != service.ServiceID)
                return NotFound();

            ModelState.Remove(nameof(Service.ServiceCategory));

            // Keep whatever the catalogue already says, so a staff edit to the
            // name or the description cannot move the price with it.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                service.DefaultPrice = await _context.Services
                    .Where(s => s.ServiceID == id)
                    .Select(s => s.DefaultPrice)
                    .FirstOrDefaultAsync();

                ModelState.Remove(nameof(Service.DefaultPrice));
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(service);
                return View(service);
            }

            try
            {
                _context.Update(service);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Services.AnyAsync(s => s.ServiceID == id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var service = await _context.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefaultAsync(s => s.ServiceID == id);

            if (service == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(service);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this service. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> BuildBlockReason(int serviceId)
        {
            var quotationLines = await _context.QuotationDetails.CountAsync(d => d.ServiceID == serviceId);
            var jobOrderLines = await _context.JobOrderDetails.CountAsync(d => d.ServiceID == serviceId);
            var invoiceLines = await _context.ServiceInvoiceDetails.CountAsync(d => d.ServiceID == serviceId);

            var total = quotationLines + jobOrderLines + invoiceLines;
            if (total == 0)
                return null;

            return $"Can't delete this service. It's used on {total} line item{(total == 1 ? "" : "s")} " +
                   "across quotations, job orders, or service invoices. Mark it inactive instead, or remove those lines first.";
        }

        private async Task LoadDropdowns(Service? service = null)
        {
            ViewData["CanEditPrice"] = AppRoles.CanEditCatalogPrices(User);

            ViewData["ServiceCategoryID"] = new SelectList(
                await _context.ServiceCategories.OrderBy(c => c.CategoryName).ToListAsync(),
                "ServiceCategoryID", "CategoryName", service?.ServiceCategoryID);
        }
    }
}