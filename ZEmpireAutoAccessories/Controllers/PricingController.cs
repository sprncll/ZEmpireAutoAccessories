using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Pricing")]
    public class PricingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPricingService _pricingService;

        public PricingController(ApplicationDbContext context, IPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        // GET: Pricing?q=...
        public async Task<IActionResult> Index(string? q)
        {
            ViewData["CanEditPrice"] = AppRoles.CanEditCatalogPrices(User);

            var pricing = await _pricingService.GetAllPricing(q);
            ViewData["Search"] = q;
            return View(pricing);
        }

        public async Task<IActionResult> Details(int? id)
        {
            ViewData["CanEditPrice"] = AppRoles.CanEditCatalogPrices(User);

            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings
                .Include(p => p.Product)
                .Include(p => p.TintVariant)
                .Include(p => p.VehicleClassification)
                .Include(p => p.Panel)
                .FirstOrDefaultAsync(p => p.PricingID == id);

            if (pricing == null)
                return NotFound();

            ViewData["History"] = await _pricingService.GetPriceHistory(id.Value);

            return View(pricing);
        }

        // GET: Pricing/Create?productId=5
        //
        // There is no "New Pricing" button any more - a price is always set
        // for a product you navigated to, either straight after creating it
        // or from the Products list. productId pre-selects and locks that
        // product so the form is only asking for the parts still unknown.
        public async Task<IActionResult> Create(int? productId)
        {
            var pricing = new Pricing { ProductID = productId ?? 0 };

            if (productId != null)
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                    return NotFound();

                ViewData["LockedProductName"] = product.ProductName;
                ViewData["LockedProductCategory"] = product.Category.CategoryName;
                ViewData["ExistingPriceCount"] =
                    await _context.Pricings.CountAsync(p => p.ProductID == productId);
            }

            await LoadDropdowns(pricing);
            return View(pricing);
        }

        /// <summary>
        /// Names the product the form is locked to, so the banner survives a
        /// failed save instead of dropping back to a bare form.
        /// </summary>
        private async Task DescribeLockedProduct(int productId)
        {
            if (productId <= 0)
                return;

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
                return;

            ViewData["LockedProductName"] = product.ProductName;
            ViewData["LockedProductCategory"] = product.Category.CategoryName;
            ViewData["ExistingPriceCount"] =
                await _context.Pricings.CountAsync(p => p.ProductID == productId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnlyPrice]
        public async Task<IActionResult> Create(
            [Bind("ProductID,TintVariantID,VehicleClassificationID,PanelID,Price")] Pricing pricing)
        {
            ModelState.Remove(nameof(Pricing.Product));
            ModelState.Remove(nameof(Pricing.VehicleClassification));
            ModelState.Remove(nameof(Pricing.Panel));

            if (!ModelState.IsValid)
            {
                await DescribeLockedProduct(pricing.ProductID);
                await LoadDropdowns(pricing);
                return View(pricing);
            }

            try
            {
                await _pricingService.CreatePricing(
                    pricing.ProductID, pricing.TintVariantID,
                    pricing.VehicleClassificationID, pricing.PanelID, pricing.Price);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await DescribeLockedProduct(pricing.ProductID);
                await LoadDropdowns(pricing);
                return View(pricing);
            }

            // A product usually needs a price per classification and panel, so
            // stay on the form for the same product rather than bouncing to
            // the list after each one.
            TempData["Success"] = "Price saved.";
            return RedirectToAction(nameof(Create), new { productId = pricing.ProductID });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings
                .Include(p => p.Product)
                .Include(p => p.TintVariant)
                .Include(p => p.VehicleClassification)
                .Include(p => p.Panel)
                .FirstOrDefaultAsync(p => p.PricingID == id);

            if (pricing == null)
                return NotFound();

            return View(pricing);
        }

        // Only the price itself can change - the Product/Variant/Classification/Panel
        // combination is fixed once created (PricingService.UpdatePrice journals the
        // change to cat.PriceHistory).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnlyPrice]
        public async Task<IActionResult> Edit(int id, decimal newPrice)
        {
            if (newPrice < 0)
                ModelState.AddModelError(string.Empty, "Price cannot be negative.");

            if (!ModelState.IsValid)
            {
                var pricing = await _context.Pricings
                    .Include(p => p.Product)
                    .Include(p => p.TintVariant)
                    .Include(p => p.VehicleClassification)
                    .Include(p => p.Panel)
                    .FirstOrDefaultAsync(p => p.PricingID == id);

                if (pricing == null)
                    return NotFound();

                return View(pricing);
            }

            try
            {
                await _pricingService.UpdatePrice(id, newPrice, CurrentUserId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var pricing = await _context.Pricings
                .Include(p => p.Product)
                .Include(p => p.TintVariant)
                .Include(p => p.VehicleClassification)
                .Include(p => p.Panel)
                .FirstOrDefaultAsync(p => p.PricingID == id);

            if (pricing == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(pricing);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnlyPrice]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pricing = await _context.Pricings.FindAsync(id);
            if (pricing == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Pricings.Remove(pricing);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this pricing record. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // Pricing is ON DELETE RESTRICT from PriceHistory, QuotationDetail and JobOrderDetail.
        private async Task<string?> BuildBlockReason(int pricingId)
        {
            var history = await _context.PriceHistories.CountAsync(h => h.PricingID == pricingId);
            var quotationLines = await _context.QuotationDetails.CountAsync(d => d.PricingID == pricingId);
            var jobOrderLines = await _context.JobOrderDetails.CountAsync(d => d.PricingID == pricingId);

            var parts = new List<string>();
            if (history > 0) { parts.Add($"{history} price change record{(history == 1 ? "" : "s")}"); }
            if (quotationLines > 0) { parts.Add($"{quotationLines} quotation line{(quotationLines == 1 ? "" : "s")}"); }
            if (jobOrderLines > 0) { parts.Add($"{jobOrderLines} job order line{(jobOrderLines == 1 ? "" : "s")}"); }

            if (parts.Count == 0)
                return null;

            return "Can't delete this pricing record. It's referenced by " + string.Join(", ", parts) + ".";
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadDropdowns(Pricing? pricing = null)
        {
            ViewData["ProductID"] = new SelectList(
                await _context.Products.Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync(),
                "ProductID", "ProductName", pricing?.ProductID);

            var tintVariants = await _context.TintVariants
                .Include(v => v.Product)
                .OrderBy(v => v.Product.ProductName).ThenBy(v => v.VariantName)
                .Select(v => new { v.TintVariantID, Display = v.Product.ProductName + " - " + v.VariantName })
                .ToListAsync();
            ViewData["TintVariantID"] = new SelectList(tintVariants, "TintVariantID", "Display", pricing?.TintVariantID);

            // Which variants belong to which product, so the form can narrow
            // the Tint Variant list to the product that was picked instead of
            // offering every variant in the catalog. cat.Pricing carries a
            // composite FK on (TintVariantID, ProductID), so a mismatched
            // pair is rejected by the database anyway - this keeps it off the
            // form in the first place.
            ViewData["VariantsByProduct"] = await _context.TintVariants
                .OrderBy(v => v.VariantName)
                .Select(v => new
                {
                    productId = v.ProductID,
                    id = v.TintVariantID,
                    name = v.VariantName
                })
                .ToListAsync();

            ViewData["VehicleClassificationID"] = new SelectList(
                await _context.VehicleClassifications.OrderBy(v => v.ClassificationName).ToListAsync(),
                "VehicleClassificationID", "ClassificationName", pricing?.VehicleClassificationID);

            ViewData["PanelID"] = new SelectList(
                await _context.Panels.OrderBy(p => p.PanelName).ToListAsync(),
                "PanelID", "PanelName", pricing?.PanelID);
        }
    }
}