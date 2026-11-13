using ZEmpireAutoAccessories.Services;
using ZEmpireAutoAccessories.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Models.Account;

namespace ZEmpireAutoAccessories.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ActivityRecorder _activity;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context,
            ActivityRecorder activity)
        {
            _context = context;
            _activity = activity;
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = model.UserNameOrEmail.Contains('@')
                ? await _userManager.FindByEmailAsync(model.UserNameOrEmail)
                : await _userManager.FindByNameAsync(model.UserNameOrEmail);

            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account is deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {User} signed in.", user.UserName);
                // This request is not signed in yet - the cookie is only on
                // its way - so who it was is said rather than looked up.
                await _activity.Note(_context, "Account", user.Id, "Signed in", user.Id, user.UserName);
                return RedirectToLocal(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                await _activity.Note(_context, "Account", user.Id, "Locked out after repeated wrong passwords", user.Id, user.UserName);
                ModelState.AddModelError(string.Empty, "Account locked out due to multiple failed attempts. Try again later.");
                return View(model);
            }

            // A wrong password for a real account. An unknown name is not
            // logged: it is whatever somebody typed, and it names nobody.
            await _activity.Note(_context, "Account", user.Id, "Failed sign-in (wrong password)", user.Id, user.UserName);

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            // Changing the password rotates the security stamp, which would
            // otherwise sign the user out on their very next request.
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Your password has been changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
