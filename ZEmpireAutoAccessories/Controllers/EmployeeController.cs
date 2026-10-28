using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("User Management")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EmployeeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            return View(employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e =>
                    e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // GET: Employee/Create
        public async Task<IActionResult> Create()
        {
            await LoadRoleDropdown();

            return View(new Employee { IsActive = true });
        }

        // POST: Employee/Create
        // Creates a brand-new login account for this employee (there's no
        // screen anywhere else in the app to create one ahead of time).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, string userName, string password, string role)
        {
            ModelState.Remove(nameof(Employee.User));
            ModelState.Remove(nameof(Employee.UserId));

            if (string.IsNullOrWhiteSpace(userName))
                ModelState.AddModelError(nameof(userName), "Login username is required.");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError(nameof(password), "Initial password is required.");

            if (string.IsNullOrWhiteSpace(role))
                ModelState.AddModelError(nameof(role), "Role is required.");

            if (!string.IsNullOrWhiteSpace(employee.EmployeeNumber) &&
                await _context.Employees.AnyAsync(e => e.EmployeeNumber == employee.EmployeeNumber))
            {
                ModelState.AddModelError(string.Empty, $"Employee number '{employee.EmployeeNumber}' is already taken.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoleDropdown(role);
                return View(employee);
            }

            var newUser = new ApplicationUser
            {
                UserName = userName.Trim(),
                FullName = $"{employee.FirstName} {employee.LastName}".Trim(),
                // Mirrors the form's Active checkbox. Login reads this flag on
                // the account, not the one on the Employee row, so creating an
                // inactive employee used to hand them a working login anyway.
                IsActive = employee.IsActive,
                CreatedAt = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadRoleDropdown(role);
                return View(employee);
            }

            var roleResult = await SetSingleRole(newUser, role);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await _userManager.DeleteAsync(newUser);
                await LoadRoleDropdown(role);
                return View(employee);
            }

            employee.UserId = newUser.Id;
            employee.CreatedAt = DateTime.UtcNow;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e =>
                    e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(employee.User);
            await LoadRoleDropdown(currentRoles.FirstOrDefault());

            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Employee employee,
            string userName,
            string role)
        {
            if (id != employee.EmployeeID)
                return NotFound();

            ModelState.Remove(nameof(Employee.User));

            if (string.IsNullOrWhiteSpace(userName))
                ModelState.AddModelError(nameof(userName), "Login username is required.");

            if (string.IsNullOrWhiteSpace(role))
                ModelState.AddModelError(nameof(role), "Role is required.");

            if (!string.IsNullOrWhiteSpace(employee.EmployeeNumber) &&
                await _context.Employees.AnyAsync(e => e.EmployeeNumber == employee.EmployeeNumber && e.EmployeeID != id))
            {
                ModelState.AddModelError(string.Empty, $"Employee number '{employee.EmployeeNumber}' is already taken.");
            }

            if (!ModelState.IsValid)
            {
                employee.User = await _context.Users.FirstOrDefaultAsync(u => u.Id == employee.UserId) ?? employee.User;
                await LoadRoleDropdown(role);
                return View(employee);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == employee.UserId);
            if (user == null)
                return NotFound();

            if (!string.Equals(user.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var result = await _userManager.SetUserNameAsync(user, userName.Trim());
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    employee.User = user;
                    await LoadRoleDropdown(role);
                    return View(employee);
                }
            }

            var roleResult = await SetSingleRole(user, role);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                employee.User = user;
                await LoadRoleDropdown(role);
                return View(employee);
            }

            // The header, the avatar initials and every "signed in as" line read
            // AspNetUsers.FullName, which until now was only ever written when
            // the account was created. Renaming an employee left the old name on
            // screen for good, so recompute it here from the same two fields
            // Create uses.
            user.FullName = $"{employee.FirstName} {employee.LastName}".Trim();

            // Setting an employee inactive has to reach the login account:
            // AccountController.Login tests AspNetUsers.IsActive, while the
            // screen's Active checkbox sets the flag on the Employee row. Until
            // these were mirrored, deactivating someone changed a badge in the
            // list and nothing else -- they could still sign in.
            var wasDeactivated = user.IsActive && !employee.IsActive;
            user.IsActive = employee.IsActive;

            employee.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(employee.EmployeeID))
                    return NotFound();

                throw;
            }

            // Blocking the next login is not enough: a deactivated employee who
            // is already signed in keeps a valid cookie for up to eight hours.
            // Rolling the security stamp invalidates it, so the next time the
            // cookie is revalidated they are signed out.
            if (wasDeactivated)
                await _userManager.UpdateSecurityStampAsync(user);

            // Name and role live in the auth cookie, which is only rebuilt every
            // half hour. Changing the username also rolls the security stamp,
            // which would sign this person out mid-edit. Reissue the cookie now.
            else if (user.Id == _userManager.GetUserId(User))
                await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e =>
                    e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            ViewData["AssignedJobOrders"] = await _context.JobOrders
                .CountAsync(j => j.AssignedEmployeeID == id);

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return RedirectToAction(nameof(Index));

            var jobOrderCount = await _context.JobOrders
                .CountAsync(j => j.AssignedEmployeeID == id);

            if (jobOrderCount > 0)
            {
                var word = jobOrderCount == 1 ? "job order" : "job orders";
                TempData["DeleteError"] =
                    $"Can't delete this employee. They are assigned to {jobOrderCount} {word}. " +
                    "Reassign those job orders first.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this employee. They still have related records elsewhere in the system.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Employee/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword, string confirmPassword)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null)
                return NotFound();

            if (newPassword != confirmPassword)
            {
                TempData["ResetPasswordError"] = "The new password and confirmation don't match.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(employee.User);
            var result = await _userManager.ResetPasswordAsync(employee.User, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["ResetPasswordError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = $"Password reset for {employee.User.UserName}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Removes any roles the user currently holds and adds the one selected -
        // this app only ever wants a user in exactly one of Admin/Staff at a time.
        private async Task<IdentityResult> SetSingleRole(ApplicationUser user, string role)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count == 1 && currentRoles[0] == role)
                return IdentityResult.Success;

            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return removeResult;
            }

            return await _userManager.AddToRoleAsync(user, role);
        }

        private async Task LoadRoleDropdown(string? selectedRole = null)
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

            ViewData["Role"] = new SelectList(roles, selectedRole);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees
                .Any(e => e.EmployeeID == id);
        }
    }
}
