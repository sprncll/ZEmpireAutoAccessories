using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// How much film each panel takes on each class of vehicle - the figures
    /// that decide what comes off a roll when a tint job is completed.
    ///
    /// One grid: vehicle classifications down, panels across, metres in the
    /// boxes. A blank box means the pairing has no cut size, and a document
    /// using it cannot be completed until it does - see DocumentStockService
    /// for why that is better than guessing.
    ///
    /// Shares the Inventory module rather than owning one, the same way stock
    /// counts do, so no sec.Module row is needed.
    /// </summary>
    [ModuleAuthorize("Inventory")]
    public class CutSizeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICutSizeStore _cutSizes;

        public CutSizeController(ApplicationDbContext context, ICutSizeStore cutSizes)
        {
            _context = context;
            _cutSizes = cutSizes;
        }

        // GET: CutSize
        public async Task<IActionResult> Index()
        {
            return View(await BuildGrid());
        }

        // POST: CutSize
        //
        // The whole grid comes back at once, as parallel flat lists - the same
        // shape the stock count sheet posts in. A blank box is a pairing with
        // no cut size and is simply left out.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            List<int> vehicleClassificationId,
            List<int> panelId,
            List<decimal?> meters)
        {
            var sizes = new List<CutSize>();

            for (var i = 0; i < vehicleClassificationId.Count && i < panelId.Count; i++)
            {
                var typed = i < meters.Count ? meters[i] : null;
                if (typed == null)
                    continue;

                if (typed < 0)
                {
                    ViewData["CutSizeError"] = "A cut size can't be negative.";
                    return View(await BuildGrid());
                }

                sizes.Add(new CutSize
                {
                    VehicleClassificationID = vehicleClassificationId[i],
                    PanelID = panelId[i],
                    // Typed in metres because that is how a roll is bought and
                    // talked about; held in centimetres because that is what
                    // stock is counted in.
                    Centimeters = UnitOfMeasure.ToBase(typed.Value, UnitOfMeasure.Meter)
                });
            }

            await _cutSizes.Save(sizes);

            TempData["Success"] = sizes.Count == 0
                ? "Cut sizes cleared."
                : $"{sizes.Count} cut size{(sizes.Count == 1 ? "" : "s")} saved.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<CutSizeGrid> BuildGrid()
        {
            var classifications = await _context.VehicleClassifications
                .OrderBy(c => c.ClassificationName)
                .ToListAsync();

            var panels = await _context.Panels
                .OrderBy(p => p.PanelName)
                .ToListAsync();

            var existing = _cutSizes.All()
                .ToDictionary(s => (s.VehicleClassificationID, s.PanelID), s => s.Meters);

            return new CutSizeGrid
            {
                Classifications = classifications,
                Panels = panels,
                Meters = existing
            };
        }
    }
}
