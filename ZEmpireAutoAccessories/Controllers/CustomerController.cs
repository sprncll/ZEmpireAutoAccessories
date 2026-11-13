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

        /// <summary>
        /// The customer already holding this number, if there is one.
        ///
        /// A mobile number belongs to one person, so it is what tells two
        /// customers of the same name apart - and if two rows carry the same
        /// number, it stops telling anybody anything. There is no unique
        /// index behind this: the column allows duplicates and the database
        /// is not being changed, so the rule lives here and the screens are
        /// the only way in.
        ///
        /// Both shapes are looked for. Rows entered before the shop moved to
        /// +63 still hold 09121231231, which is the same number as
        /// +639121231231 and must not be allowed to sit beside it.
        /// </summary>
        private async Task<Customer?> NumberAlreadyUsed(string number, int? exceptCustomerId = null)
        {
            var dialled = PhoneNumber.ToDialledForm(number);

            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    (c.ContactNumber == number || c.ContactNumber == dialled)
                    && (exceptCustomerId == null || c.CustomerID != exceptCustomerId));
        }

        // POST: Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ContactNumber")] Customer customer, string? lastName, string? firstName)
        {
            // The form asks for the name in two boxes; the column holds one.
            // Combined before anything is judged, so the validation below is
            // about the name that will actually be stored.
            customer.FullName = PersonName.Combine(firstName, lastName);
            ModelState.Remove(nameof(Customer.FullName));

            if (string.IsNullOrWhiteSpace(firstName))
                ModelState.AddModelError("firstName", "First name is required.");

            if (string.IsNullOrWhiteSpace(lastName))
                ModelState.AddModelError("lastName", "Last name is required.");

            // Normalise before judging it: binding has already run the regex
            // against the raw text, so clear that verdict and test the stored
            // shape instead.
            customer.ContactNumber = PhoneNumber.ToPhilippineMobile(customer.ContactNumber);
            ModelState.Remove(nameof(Customer.ContactNumber));

            if (customer.ContactNumber is null)
            {
                ModelState.AddModelError(nameof(Customer.ContactNumber),
                    "Enter a Philippine mobile number: +63 then 10 digits starting with 9.");
            }
            else
            {
                // Named, not just refused: nine times out of ten this is the
                // same customer being entered twice, and whoever is typing
                // needs to know whose record to open instead.
                var holder = await NumberAlreadyUsed(customer.ContactNumber);
                if (holder != null)
                    ModelState.AddModelError(nameof(Customer.ContactNumber),
                        $"That number is already on file for \"{holder.FullName}\". " +
                        "A mobile number belongs to one customer, so open their record instead.");
            }

            if (!ModelState.IsValid)
            {
                // Hand the two boxes back as they were typed, so a wrong
                // number does not cost somebody their customer's name too.
                ViewData["LastName"] = lastName;
                ViewData["FirstName"] = firstName;
                return View(customer);
            }

            customer.CreatedAt = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Customer \"{customer.FullName}\" was added. Now add their vehicle.";
            // The new vehicle form's Back, Cancel and Save all return to the
            // customer just made - not to the blank New Customer form, which
            // would only invite saving the same person twice, and not to the
            // vehicle list, which nobody was looking at.
            return RedirectToAction("Create", "Vehicle", new
            {
                customerId = customer.CustomerID,
                returnUrl = Url.Action(nameof(Details), new { id = customer.CustomerID })
            });
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
                return NotFound();

            var (first, last) = PersonName.Split(customer.FullName);
            ViewData["FirstName"] = first;
            ViewData["LastName"] = last;

            return View(customer);
        }

        // POST: Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, [Bind("CustomerID,ContactNumber")] Customer customer,
            string? lastName, string? firstName)
        {
            if (id != customer.CustomerID)
                return NotFound();

            customer.FullName = PersonName.Combine(firstName, lastName);
            ModelState.Remove(nameof(Customer.FullName));

            if (string.IsNullOrWhiteSpace(firstName))
                ModelState.AddModelError("firstName", "First name is required.");

            if (string.IsNullOrWhiteSpace(lastName))
                ModelState.AddModelError("lastName", "Last name is required.");

            // Normalise before judging it: binding has already run the regex
            // against the raw text, so clear that verdict and test the stored
            // shape instead.
            customer.ContactNumber = PhoneNumber.ToPhilippineMobile(customer.ContactNumber);
            ModelState.Remove(nameof(Customer.ContactNumber));

            if (customer.ContactNumber is null)
            {
                ModelState.AddModelError(nameof(Customer.ContactNumber),
                    "Enter a Philippine mobile number: +63 then 10 digits starting with 9.");
            }
            else
            {
                // Everyone except this customer, so re-saving a record without
                // touching the number is not refused by its own number.
                var holder = await NumberAlreadyUsed(customer.ContactNumber, id);
                if (holder != null)
                    ModelState.AddModelError(nameof(Customer.ContactNumber),
                        $"That number is already on file for \"{holder.FullName}\". " +
                        "A mobile number belongs to one customer.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["LastName"] = lastName;
                ViewData["FirstName"] = firstName;
                return View(customer);
            }

            // Load the tracked entity and update only the editable fields so
            // CreatedAt (and anything else) is preserved and over-posting is impossible.
            var existing = await _context.Customers.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.FullName = customer.FullName;
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