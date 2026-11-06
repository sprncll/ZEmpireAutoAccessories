using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Customers")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customer?q=...
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Customers
                .Include(c => c.Vehicles)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(term) ||
                    (c.ContactNumber != null && c.ContactNumber.Contains(term)));
            }

            var customers = await query
                .OrderBy(c => c.FullName)
                .ToListAsync();

            ViewData["Search"] = q;
            return View(customers);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                    .ThenInclude(v => v.VehicleClassification)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,ContactNumber")] Customer customer)
        {
            // Normalise before judging it: binding has already run the regex
            // against the raw text, so clear that verdict and test the stored
            // shape instead.
            customer.ContactNumber = PhoneNumber.ToPhilippineMobile(customer.ContactNumber);
            ModelState.Remove(nameof(Customer.ContactNumber));

            if (customer.ContactNumber is null)
                ModelState.AddModelError(nameof(Customer.ContactNumber),
                    "Enter a Philippine mobile number: +63 then 10 digits starting with 9.");

            if (!ModelState.IsValid)
                return View(customer);

            customer.FullName = customer.FullName.Trim();
            customer.CreatedAt = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Customer \"{customer.FullName}\" was added. Now add their vehicle.";
            return RedirectToAction("Create", "Vehicle", new { customerId = customer.CustomerID });
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerID,FullName,ContactNumber")] Customer customer)
        {
            if (id != customer.CustomerID)
                return NotFound();

            // Normalise before judging it: binding has already run the regex
            // against the raw text, so clear that verdict and test the stored
            // shape instead.
            customer.ContactNumber = PhoneNumber.ToPhilippineMobile(customer.ContactNumber);
            ModelState.Remove(nameof(Customer.ContactNumber));

            if (customer.ContactNumber is null)
                ModelState.AddModelError(nameof(Customer.ContactNumber),
                    "Enter a Philippine mobile number: +63 then 10 digits starting with 9.");

            if (!ModelState.IsValid)
                return View(customer);

            // Load the tracked entity and update only the editable fields so
            // CreatedAt (and anything else) is preserved and over-posting is impossible.
            var existing = await _context.Customers.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.FullName = customer.FullName.Trim();
            existing.ContactNumber = customer.ContactNumber;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Customer \"{existing.FullName}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Vehicles)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return RedirectToAction(nameof(Index));

            // Vehicles use ON DELETE RESTRICT - block instead of throwing a DB error.
            if (customer.Vehicles.Count > 0)
            {
                var word = customer.Vehicles.Count == 1 ? "vehicle" : "vehicles";
                TempData["DeleteError"] =
                    $"Can't delete \"{customer.FullName}\" - they still have {customer.Vehicles.Count} {word} registered. " +
                    $"Remove or reassign the {word} first.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this customer. They still have related records " +
                    "(sales, job orders, quotations, or invoices) elsewhere in the system. " +
                    "Remove or reassign those first.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["Success"] = $"Customer \"{customer.FullName}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers
                .Any(c => c.CustomerID == id);
        }
    }
}