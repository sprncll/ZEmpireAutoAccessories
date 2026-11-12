using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// The figures that were written into the code: the shop's name and
    /// address, its TIN, the VAT rate, and the points at which stock counts
    /// as low.
    ///
    /// Administrators only, and not a module. Module access decides which of
    /// the shop's SCREENS somebody can open; this changes what every screen
    /// says - the rate on every receipt, the warning on every stock row - so
    /// it belongs with the roles rather than beside the day's work.
    /// </summary>
    [AdminOnly]
    public class SettingsController : Controller
    {
        private readonly ISettingsStore _settings;

        public SettingsController(ISettingsStore settings)
        {
            _settings = settings;
        }

        public IActionResult Index() => View(_settings.Current);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ShopSettings settings)
        {
            if (!ModelState.IsValid)
                return View(settings);

            // A rate of zero is a real answer - a shop that is not VAT
            // registered - so it is allowed. A low-stock point of zero is
            // not: it would mean nothing is ever low until it is gone, which
            // is what the warning exists to prevent.
            if (settings.LowStockPieces <= 0)
                ModelState.AddModelError(nameof(ShopSettings.LowStockPieces),
                    "A counted product has to warn at something above zero, or it only warns when it is gone.");

            if (settings.LowStockMetres <= 0)
                ModelState.AddModelError(nameof(ShopSettings.LowStockMetres),
                    "A roll has to warn at something above zero, or it only warns when it is finished.");

            if (!ModelState.IsValid)
                return View(settings);

            await _settings.Save(settings);

            TempData["Success"] = "Settings saved. They take effect everywhere straight away.";
            return RedirectToAction(nameof(Index));
        }
    }
}
