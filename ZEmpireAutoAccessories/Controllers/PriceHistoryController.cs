using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;

namespace ZEmpireAutoAccessories.Controllers
{
    // Read-only audit log of every price change across all Pricing records.
    // Kept separate from PricingController since "Price History" is its own
    // grantable module - some roles may need to review price changes without
    // being able to edit pricing itself.
    [ModuleAuthorize("Price History")]
    public class PriceHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PriceHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? page = null)
        {
            var history = await _context.PriceHistories
                .Include(h => h.User)
                .Include(h => h.Pricing)
                    .ThenInclude(p => p.Product)
                .Include(h => h.Pricing)
                    .ThenInclude(p => p.VehicleClassification)
                .Include(h => h.Pricing)
                    .ThenInclude(p => p.Panel)
                .OrderByDescending(h => h.DateChanged)
                .ToPageAsync(page, ViewData);

            return View(history);
        }
    }
}