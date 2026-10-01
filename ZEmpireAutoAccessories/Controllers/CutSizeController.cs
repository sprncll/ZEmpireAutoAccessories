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

        // GET: CutSize?unit=cm
        //
        // The unit is a round trip rather than a bit of arithmetic in the
        // browser: the server renders the figures in it and reads them back in
        // it, so there is one conversion, in one place, and no chance of the
        // screen and the save disagreeing.
        public async Task<IActionResult> Index(string? unit)
        {
            return View(await BuildGrid(unit));
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
            List<decimal?> size,
            string? unit)
        {
            var chosen = Unit(unit);
            var sizes = new List<CutSize>();

            for (var i = 0; i < vehicleClassificationId.Count && i < panelId.Count; i++)
            {
                var typed = i < size.Count ? size[i] : null;
                if (typed == null)
                    continue;

                if (typed < 0)
                {
                    ViewData["CutSizeError"] = "A cut size can't be negative.";
                    return View(await BuildGrid(chosen));
                }

                sizes.Add(new CutSize
                {
                    VehicleClassificationID = vehicleClassificationId[i],
                    PanelID = panelId[i],
                    // Typed in whatever the shop measures in; held in
                    // centimetres, because that is what stock is counted in.
                    Centimeters = UnitOfMeasure.ToBase(typed.Value, chosen)
                });
            }

            await _cutSizes.Save(sizes);

            TempData["Success"] = sizes.Count == 0
                ? "Cut sizes cleared."
                : $"{sizes.Count} cut size{(sizes.Count == 1 ? "" : "s")} saved.";

            return RedirectToAction(nameof(Index), new { unit = chosen });
        }

        // POST: CutSize/FillSuggested
        //
        // Puts a starting figure in every blank box it has one for, and leaves
        // every box that already has a number alone - a fill must never quietly
        // overwrite a size somebody measured.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FillSuggested(string? unit)
        {
            var grid = await BuildGrid(unit);

            var filled = grid.Classifications
                .SelectMany(c => grid.Panels.Select(p => new { Class = c, Panel = p }))
                .Select(pair => new
                {
                    pair.Class,
                    pair.Panel,
                    Centimeters = grid.Centimeters.ContainsKey(
                        (pair.Class.VehicleClassificationID, pair.Panel.PanelID))
                        ? null
                        : SuggestedCutSizes.Centimeters(
                            pair.Class.ClassificationName, pair.Panel.PanelName)
                })
                .Where(x => x.Centimeters != null)
                .Select(x => new CutSize
                {
                    VehicleClassificationID = x.Class.VehicleClassificationID,
                    PanelID = x.Panel.PanelID,
                    Centimeters = x.Centimeters!.Value
                })
                .ToList();

            if (filled.Count == 0)
            {
                TempData["Success"] = "Nothing to fill - every panel the glass sheet covers already has a size.";
                return RedirectToAction(nameof(Index), new { unit = Unit(unit) });
            }

            // Everything already on record, plus the blanks just filled.
            var all = grid.Centimeters
                .Select(entry => new CutSize
                {
                    VehicleClassificationID = entry.Key.VehicleClassificationID,
                    PanelID = entry.Key.PanelID,
                    Centimeters = entry.Value
                })
                .Concat(filled)
                .ToList();

            await _cutSizes.Save(all);

            TempData["Success"] =
                $"Filled {filled.Count} blank box{(filled.Count == 1 ? "" : "es")} from the shop's glass sheet, " +
                $"worked out for {SuggestedCutSizes.ConfiguredRollWidthCm:0.##} cm film.";

            return RedirectToAction(nameof(Index), new { unit = Unit(unit) });
        }

        /// <summary>
        /// The unit asked for, or metres. A length unit only - anything else
        /// would be read as centimetres by UnitOfMeasure and quietly mean
        /// something other than what the screen said.
        /// </summary>
        private static string Unit(string? unit) =>
            UnitOfMeasure.IsLengthUnit(unit) ? unit! : UnitOfMeasure.Meter;

        private async Task<CutSizeGrid> BuildGrid(string? unit)
        {
            var classifications = await _context.VehicleClassifications
                .OrderBy(c => c.ClassificationName)
                .ToListAsync();

            var panels = await _context.Panels
                .OrderBy(p => p.PanelName)
                .ToListAsync();

            var existing = _cutSizes.All()
                .ToDictionary(s => (s.VehicleClassificationID, s.PanelID), s => s.Centimeters);

            // Which pairings a film product is actually priced for. The
            // add-line forms build their panel list from cat.Pricing, so a
            // pairing with no price there can never reach a document and its
            // cut size would never be read - asking for one is busywork, and
            // counting it as missing hides the ones that will really stop a
            // job. Only roll goods matter: a coating priced against Whole
            // Vehicle is a liquid and takes nothing off a roll.
            var sold = await _context.Pricings
                .Where(p => p.Product.Category != null)
                .Select(p => new
                {
                    p.VehicleClassificationID,
                    p.PanelID,
                    p.ProductID,
                    p.Product.CategoryID,
                    CategoryName = p.Product.Category.CategoryName
                })
                .Distinct()
                .ToListAsync();

            return new CutSizeGrid
            {
                Classifications = classifications,
                Panels = panels,
                Centimeters = existing,
                Unit = Unit(unit),
                Sold = sold
                    .Where(p => UnitOfMeasure.ForProduct(
                        p.ProductID, p.CategoryID, p.CategoryName))
                    .Select(p => (p.VehicleClassificationID, p.PanelID))
                    .ToHashSet()
            };
        }
    }
}
