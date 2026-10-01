using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Products")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMeasureStore _measures;

        public ProductController(ApplicationDbContext context, IMeasureStore measures)
        {
            _context = context;
            _measures = measures;
        }

        // GET: Product
        public async Task<IActionResult> Index(string? q)
        {
            ViewData["CanEditPrice"] = AppRoles.CanEditCatalogPrices(User);

            var all = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            // Grouped by the canonical category, so a shelf with two names is
            // one band. Sorted after that resolution, or the band would appear
            // twice under the same heading.
            var products = all
                .OrderBy(p => ProductCategories.Canonical(p.Category.CategoryName))
                .ThenBy(p => p.ProductName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                products = products
                    .Where(p => p.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase)
                             || ProductCategories.Canonical(p.Category.CategoryName)
                                    .Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            ViewData["Search"] = q;

            // How many prices each product has, so the list can flag the ones
            // that still need one - a product with no cat.Pricing row cannot
            // be put on a quotation or job order.
            ViewData["PriceCountByProduct"] = await _context.Pricings
                .GroupBy(p => p.ProductID)
                .Select(g => new { ProductID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProductID, x => x.Count);

            // One heading per shelf: a product filed under a duplicate category
            // name is listed under the canonical one.
            ViewData["CategoryDisplay"] = products.ToDictionary(
                p => p.ProductID, p => ProductCategories.Canonical(p.Category.CategoryName));

            // Which products are measured off a roll rather than counted, so
            // the list says so rather than leaving it implied by the category.
            ViewData["LengthProducts"] = products
                .Where(p => UnitOfMeasure.ForProduct(
                    p.ProductID, p.CategoryID, p.Category.CategoryName))
                .Select(p => p.ProductID)
                .ToHashSet();

            return View(products);
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Pricings)
                .FirstOrDefaultAsync(p =>
                    p.ProductID == id);

            if (product == null)
                return NotFound();

            ViewData["SoldByLength"] = UnitOfMeasure.ForProduct(
                product.ProductID, product.CategoryID, product.Category.CategoryName);
            ViewData["ProductSoldByLength"] = _measures.ForProduct(product.ProductID);
            ViewData["CategoryDisplay"] = ProductCategories.Canonical(product.Category.CategoryName);

            return View(product);
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, bool? soldByLength)
        {
            ModelState.Remove(nameof(Product.Category));

            // The form renders this read-only for staff, but a read-only field
            // still posts and a form can be replayed, so the value is dropped
            // here rather than trusted. A new product simply starts with no
            // default price, which an administrator can then set.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                product.DefaultPrice = null;
                ModelState.Remove(nameof(Product.DefaultPrice));
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(product, soldByLength);
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Stored after the insert, because the id is what it is keyed by.
            // Null is the default and stores nothing, leaving the product to
            // follow its category exactly as it would have before the picker.
            await _measures.SetProduct(product.ProductID, soldByLength);

            // A product with no price cannot be sold or quoted, so go straight
            // on to setting one instead of leaving that to be remembered later.
            // Someone without the Pricing module can't be sent there, so they
            // land back on the list as before.
            if (User.HasClaim(AppClaims.ModuleAccess, "Pricing"))
            {
                TempData["Success"] =
                    $"\"{product.ProductName}\" saved. Now set its price.";

                return RedirectToAction(
                    "Create", "Pricing", new { productId = product.ProductID });
            }

            TempData["Success"] = $"\"{product.ProductName}\" saved.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product =
                await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            await LoadDropdowns(product, _measures.ForProduct(product.ProductID));

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            bool? soldByLength)
        {
            if (id != product.ProductID)
                return NotFound();

            ModelState.Remove(nameof(Product.Category));

            // Keep whatever the catalogue already says, so a staff edit to the
            // name or the description cannot move the price with it.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                product.DefaultPrice = await _context.Products
                    .Where(p => p.ProductID == id)
                    .Select(p => p.DefaultPrice)
                    .FirstOrDefaultAsync();

                ModelState.Remove(nameof(Product.DefaultPrice));
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(product, soldByLength);
                return View(product);
            }

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductID))
                    return NotFound();

                throw;
            }

            await _measures.SetProduct(product.ProductID, soldByLength);

            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p =>
                    p.ProductID == id);

            if (product == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(product);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this product. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Otherwise the setting outlives the product, and attaches itself
            // to whatever the database next hands this id out to.
            await _measures.ForgetProduct(id);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// cat.Product is the parent of eight ON DELETE RESTRICT foreign keys,
        /// so deleting one that is referenced anywhere is refused by the
        /// database. Say which rows are holding it instead of letting that
        /// surface as an unhandled DbUpdateException.
        /// </summary>
        private async Task<string?> BuildBlockReason(int productId)
        {
            var pricing = await _context.Pricings.CountAsync(p => p.ProductID == productId);
            var variants = await _context.TintVariants.CountAsync(v => v.ProductID == productId);

            var quotationLines = await _context.QuotationDetails.CountAsync(d => d.ProductID == productId);
            var jobOrderLines = await _context.JobOrderDetails.CountAsync(d => d.ProductID == productId);
            var saleLines = await _context.SalesDetails.CountAsync(d => d.ProductID == productId);
            var invoiceLines = await _context.ServiceInvoiceDetails.CountAsync(d => d.ProductID == productId);

            var stockChecks = await _context.InventoryCheckDetails.CountAsync(d => d.ProductID == productId);
            var stockMoves = await _context.InventoryTransactions.CountAsync(t => t.ProductID == productId);

            var parts = new List<string>();
            if (pricing > 0) { parts.Add($"{pricing} pricing record{(pricing == 1 ? "" : "s")}"); }
            if (variants > 0) { parts.Add($"{variants} tint variant{(variants == 1 ? "" : "s")}"); }

            var lines = quotationLines + jobOrderLines + saleLines + invoiceLines;
            if (lines > 0) { parts.Add($"{lines} line item{(lines == 1 ? "" : "s")}"); }

            var stock = stockChecks + stockMoves;
            if (stock > 0) { parts.Add($"{stock} inventory record{(stock == 1 ? "" : "s")}"); }

            if (parts.Count == 0)
                return null;

            return "Can't delete this product. It's referenced by " + string.Join(", ", parts) +
                   ". Mark it inactive instead, or remove those records first.";
        }

        private async Task LoadDropdowns(Product? product = null, bool? soldByLength = null)
        {
            ViewData["ProductSoldByLength"] = soldByLength;
            ViewData["CanEditPrice"] = AppRoles.CanEditCatalogPrices(User);

            var categories = await _context.ProductCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // Duplicate category names are kept off the form so nothing new is
            // filed under one - but a product already sitting in one keeps it
            // on the list, or editing that product would silently move it.
            var offered = categories
                .Where(c => !ProductCategories.IsDuplicate(c.CategoryName)
                         || c.CategoryID == product?.CategoryID)
                .ToList();

            ViewData["CategoryID"] = new SelectList(
                offered, "CategoryID", "CategoryName", product?.CategoryID);

            // What each category would make this product, so the form can show
            // the consequence as soon as a category is picked instead of
            // leaving it to be discovered at the stock screen. Only used for
            // the "follow the category" option; a product set on its own
            // ignores it.
            ViewData["LengthCategories"] = offered
                .Where(c => UnitOfMeasure.ForCategory(c.CategoryID, c.CategoryName))
                .Select(c => c.CategoryID)
                .ToHashSet();
        }

        private bool ProductExists(int id)
        {
            return _context.Products
                .Any(p => p.ProductID == id);
        }
    }
}