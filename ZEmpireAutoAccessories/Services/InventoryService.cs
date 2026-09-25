using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Stock is transaction-based: there is no CurrentStock column. Stock-on-hand
    /// is SUM(IN) - SUM(OUT) over inv.InventoryTransaction (see dbo.vw_StockOnHand).
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetProduct(int productId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId);
        }

        public async Task<List<Product>> GetAllProducts()
        {
            return await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<decimal> GetStockOnHand(int productId)
        {
            // SUM() over zero matching rows (a product with no transactions
            // yet) comes back as SQL NULL, not 0 - project through a
            // nullable decimal so EF can represent that, then default it.
            return await _context.InventoryTransactions
                .Where(t => t.ProductID == productId)
                .SumAsync(t => (decimal?)(t.TransactionType == "IN" ? t.Quantity : -t.Quantity)) ?? 0;
        }

        public async Task<List<VwStockOnHand>> GetStockLevels()
        {
            return await _context.StockOnHand
                .OrderBy(s => s.ProductName)
                .ToListAsync();
        }

        public async Task StockIn(int productId, decimal quantity, string userId, string? unit = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Stock-in quantity must be greater than zero.");

            await EnsureProductExists(productId);
            quantity = await InBaseUnit(productId, quantity, unit);

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductID = productId,
                UserId = userId,
                TransactionType = "IN",
                Quantity = quantity,
                TransactionDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task StockOut(int productId, decimal quantity, string userId, string? unit = null)
        {
            if (quantity <= 0)
                throw new ArgumentException("Stock-out quantity must be greater than zero.");

            await EnsureProductExists(productId);
            quantity = await InBaseUnit(productId, quantity, unit);

            var onHand = await GetStockOnHand(productId);
            if (onHand < quantity)
            {
                var byLength = (await SoldByLength(new[] { productId })).Count > 0;
                throw new InvalidOperationException(
                    $"Not enough stock - only {UnitOfMeasure.Describe(onHand, byLength)} on hand, " +
                    $"{UnitOfMeasure.Describe(quantity, byLength)} requested.");
            }

            _context.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductID = productId,
                UserId = userId,
                TransactionType = "OUT",
                Quantity = quantity,
                TransactionDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task PostDocumentStock(
            IReadOnlyCollection<DocumentStockLine> lines, bool consume, string userId)
        {
            // A document can carry the same product on more than one line, and
            // the stock check has to see the total rather than each line on its
            // own - two lines of 3 against 5 in stock is short, even though
            // neither line is.
            // Roll goods are entered in whatever unit suited the job, so bring
            // every line to centimetres before anything is compared or summed.
            // A line with no unit is already a base quantity.
            var soldByLength = await SoldByLength(lines.Select(l => l.ProductID));

            var byProduct = lines
                .Where(l => l.Quantity > 0)
                .Select(l => new
                {
                    l.ProductID,
                    Quantity = soldByLength.Contains(l.ProductID)
                        ? UnitOfMeasure.ToBase(l.Quantity, l.Unit)
                        : l.Quantity
                })
                .GroupBy(l => l.ProductID)
                .Select(g => new { ProductID = g.Key, Quantity = g.Sum(l => l.Quantity) })
                .ToList();

            if (byProduct.Count == 0)
                return;

            if (consume)
            {
                // Check every line before writing any, so a document that is
                // short on its last product doesn't half-post.
                foreach (var line in byProduct)
                {
                    var onHand = await GetStockOnHand(line.ProductID);
                    if (onHand >= line.Quantity)
                        continue;

                    var name = (await GetProduct(line.ProductID))?.ProductName
                        ?? $"Product {line.ProductID}";

                    var byLength = soldByLength.Contains(line.ProductID);

                    throw new InvalidOperationException(onHand <= 0
                        ? $"{name} is out of stock."
                        : $"Not enough stock for {name} - only " +
                          $"{UnitOfMeasure.Describe(onHand, byLength)} left, " +
                          $"{UnitOfMeasure.Describe(line.Quantity, byLength)} needed.");
                }
            }

            foreach (var line in byProduct)
            {
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductID = line.ProductID,
                    UserId = userId,
                    TransactionType = consume ? "OUT" : "IN",
                    Quantity = line.Quantity,
                    TransactionDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasSufficientStock(int productId, decimal quantity)
        {
            return await GetStockOnHand(productId) >= quantity;
        }

        public async Task<List<InventoryTransaction>> GetTransactions(int productId)
        {
            return await _context.InventoryTransactions
                .Include(t => t.User)
                .Include(t => t.Product)
                .Where(t => t.ProductID == productId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        /// <summary>
        /// A quantity typed against one product, converted to the unit stock
        /// is held in. Only roll goods convert; everything else is already
        /// counted in the unit it is stored in.
        /// </summary>
        private async Task<decimal> InBaseUnit(int productId, decimal quantity, string? unit)
        {
            if (unit == null)
                return quantity;

            var byLength = await SoldByLength(new[] { productId });
            return byLength.Contains(productId)
                ? UnitOfMeasure.ToBase(quantity, unit)
                : quantity;
        }

        /// <summary>
        /// Which of these products are roll goods, so their quantities need
        /// converting to the base unit. One query rather than one per line.
        /// </summary>
        public async Task<HashSet<int>> SoldByLength(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0)
                return new HashSet<int>();

            var rows = await _context.Products
                .Where(p => ids.Contains(p.ProductID))
                .Select(p => new { p.ProductID, p.Category.CategoryName })
                .ToListAsync();

            return rows
                .Where(r => UnitOfMeasure.IsSoldByLength(r.CategoryName))
                .Select(r => r.ProductID)
                .ToHashSet();
        }

        private async Task EnsureProductExists(int productId)
        {
            if (!await _context.Products.AnyAsync(p => p.ProductID == productId))
                throw new KeyNotFoundException("Product was not found.");
        }
    }
}
