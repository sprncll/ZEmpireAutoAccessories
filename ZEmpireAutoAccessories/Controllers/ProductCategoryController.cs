using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    /// <summary>
    /// The shelves products are filed under. There was no screen for these at
    /// all - categories only ever arrived through the seed scripts, so adding
    /// a product for a line the shop had just taken on meant writing SQL.
    ///
    /// Sits under the Products module rather than one of its own: anyone
    /// allowed to add a product needs somewhere to put it, and a new module
    /// would mean a row in sec.Module, which is a schema change away from
    /// anything the app can do on its own.
    /// </summary>
    [ModuleAuthorize("Products")]
    public class ProductCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMeasureStore _measures;

        public ProductCategoryController(ApplicationDbContext context, IMeasureStore measures)
        {
            _context = context;
            _measures = measures;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // How many products each shelf holds, so the list says which ones
            // are in use and Delete can be offered honestly.
            ViewData["ProductCount"] = await _context.Products
                .GroupBy(p => p.CategoryID)
                .Select(g => new { CategoryID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryID, x => x.Count);

            ViewData["SoldByLength"] = categories
                .Where(c => UnitOfMeasure.ForCategory(c.CategoryID, c.CategoryName))
                .Select(c => c.CategoryID)
                .ToHashSet();

            return View(categories);
        }

        public IActionResult Create()
        {
            ViewData["SoldByLength"] = false;
            return View(new ProductCategory());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory category, bool soldByLength)
        {
            ModelState.Remove(nameof(ProductCategory.Products));

            await RejectDuplicateName(category);

            if (!ModelState.IsValid)
            {
                ViewData["SoldByLength"] = soldByLength;
                return View(category);
            }

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();

            // Saved after the insert, because the id is what it is keyed by.
            await _measures.SetCategory(category.CategoryID, soldByLength);

            TempData["Success"] = $"\"{category.CategoryName}\" added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            ViewData["SoldByLength"] =
                UnitOfMeasure.ForCategory(category.CategoryID, category.CategoryName);

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCategory category, bool soldByLength)
        {
            if (id != category.CategoryID)
                return NotFound();

            ModelState.Remove(nameof(ProductCategory.Products));

            await RejectDuplicateName(category);

            if (!ModelState.IsValid)
            {
                ViewData["SoldByLength"] = soldByLength;
                return View(category);
            }

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ProductCategories.AnyAsync(c => c.CategoryID == id))
                    return NotFound();

                throw;
            }

            await _measures.SetCategory(category.CategoryID, soldByLength);

            TempData["Success"] = $"\"{category.CategoryName}\" saved.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.ProductCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this category. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Otherwise its setting would sit in the file forever, and worse,
            // would attach itself to whatever category the database next hands
            // out that id to.
            await _measures.ForgetCategory(id);

            TempData["Success"] = $"\"{category.CategoryName}\" deleted.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// cat.ProductCategory has no unique index on the name, so two shelves
        /// can be called the same thing and every product screen then shows a
        /// category picker with two identical entries. Refuse it on the form
        /// instead - which is also the only place it can be refused.
        /// </summary>
        private async Task RejectDuplicateName(ProductCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.CategoryName))
                return;

            var name = category.CategoryName.Trim();

            var taken = await _context.ProductCategories
                .AnyAsync(c => c.CategoryID != category.CategoryID && c.CategoryName == name);

            if (taken)
                ModelState.AddModelError(
                    nameof(ProductCategory.CategoryName),
                    $"There is already a category called \"{name}\".");
        }

        /// <summary>
        /// A category holding products cannot go: cat.Product's foreign key
        /// restricts the delete, so the database would refuse it anyway. Say
        /// which products are holding it rather than letting that surface as
        /// an unhandled DbUpdateException.
        /// </summary>
        private async Task<string?> BuildBlockReason(int categoryId)
        {
            var products = await _context.Products.CountAsync(p => p.CategoryID == categoryId);

            if (products == 0)
                return null;

            return $"Can't delete this category. {products} product{(products == 1 ? " is" : "s are")} " +
                   "filed under it. Move them to another category first.";
        }
    }
}
