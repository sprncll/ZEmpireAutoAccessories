using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Records a product sale (sales.Sales + sales.SalesDetail) and posts an
    /// inventory OUT transaction per line. Line SubTotal is a computed column
    /// in the database, so only Quantity and UnitPrice are set here.
    /// </summary>
    public class SalesService : ISalesService
    {
        private readonly ApplicationDbContext _context;

        public SalesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Sale?> GetSale(int saleId)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Vehicle)
                .Include(s => s.User)
                .Include(s => s.PaymentMode)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.SalesID == saleId);
        }

        public async Task<List<Sale>> GetSales(string? q = null, int? productId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null)
        {
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.PaymentMode)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(s =>
                    s.InvoiceNumber.Contains(term) ||
                    s.Customer.FullName.Contains(term));
            }

            if (productId.HasValue)
                query = query.Where(s => s.SaleDetails.Any(d => d.ProductID == productId.Value));

            if (dateFrom.HasValue)
                query = query.Where(s => s.SalesDate >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));

            if (dateTo.HasValue)
                query = query.Where(s => s.SalesDate < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

            return await query
                .OrderByDescending(s => s.SalesDate)
                .ToListAsync();
        }

        public async Task<int> GetUnitsSold(int productId, DateOnly? dateFrom = null, DateOnly? dateTo = null)
        {
            var query = _context.SalesDetails
                .Where(d => d.ProductID == productId)
                .AsQueryable();

            if (dateFrom.HasValue)
                query = query.Where(d => d.Sale.SalesDate >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));

            if (dateTo.HasValue)
                query = query.Where(d => d.Sale.SalesDate < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

            // SUM() over zero matching rows (nothing sold in range) comes
            // back as SQL NULL, not 0 - project through a nullable int so
            // EF can represent that, then default it.
            return await query.SumAsync(d => (int?)d.Quantity) ?? 0;
        }

        public async Task<Sale> CreateSale(
            string userId,
            int customerId,
            int paymentModeId,
            string invoiceNumber,
            int? vehicleId,
            List<SaleLineRequest> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("A sale must contain at least one item.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (!await _context.Customers.AnyAsync(c => c.CustomerID == customerId))
                    throw new KeyNotFoundException("Customer was not found.");

                var sale = new Sale
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerID = customerId,
                    VehicleID = vehicleId,
                    UserId = userId,
                    PaymentModeID = paymentModeId,
                    SalesDate = DateTime.Now,
                    TotalAmount = 0
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                decimal total = 0;

                // Film and PPF are sold off the roll by the metre, so the line
                // quantity is metres and the price is per metre - but stock is
                // held in centimetres. Convert for the check and the movement
                // while leaving the line itself in the unit it was priced in.
                var soldByLength = await SoldByLength(items.Select(i => i.ProductID));

                foreach (var item in items)
                {
                    if (item.Quantity <= 0)
                        throw new ArgumentException("Sale quantity must be greater than zero.");

                    // CK_SalesDetail_Price rejects a negative unit price, and
                    // CK_Sales_Total rejects the negative total it would roll
                    // up into. The form's min="0" is client-side only.
                    if (item.UnitPrice < 0)
                        throw new ArgumentException("Sale unit price can't be negative.");

                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductID == item.ProductID)
                        ?? throw new KeyNotFoundException($"Product ID {item.ProductID} was not found.");

                    // SUM() over zero matching rows (a product never
                    // stocked in/out yet) comes back as SQL NULL, not 0 -
                    // project through a nullable decimal so EF can
                    // represent that, then default it.
                    var stockOnHand = await _context.InventoryTransactions
                        .Where(t => t.ProductID == item.ProductID)
                        .SumAsync(t => (decimal?)(t.TransactionType == "IN" ? t.Quantity : -t.Quantity)) ?? 0;

                    var byLength = soldByLength.Contains(item.ProductID);

                    // A line of "3" means 3 m of film, or 3 of anything else.
                    var takenFromStock = byLength
                        ? UnitOfMeasure.ToBase(item.Quantity, UnitOfMeasure.Meter)
                        : item.Quantity;

                    if (stockOnHand < takenFromStock)
                        throw new InvalidOperationException(
                            stockOnHand <= 0
                                ? $"{product.ProductName} is out of stock."
                                : $"Not enough stock for {product.ProductName} - only " +
                                  $"{UnitOfMeasure.Describe(stockOnHand, byLength)} left, " +
                                  $"{UnitOfMeasure.Describe(takenFromStock, byLength)} requested.");

                    _context.SalesDetails.Add(new SaleDetail
                    {
                        SalesID = sale.SalesID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                        // SubTotal is a computed column in the database
                    });

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductID = item.ProductID,
                        UserId = userId,
                        TransactionType = "OUT",
                        Quantity = takenFromStock,
                        TransactionDate = DateTime.Now
                    });

                    total += item.UnitPrice * item.Quantity;
                }

                sale.TotalAmount = total;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return sale;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Of these products, the ones measured by length. Mirrors
        /// InventoryService.SoldByLength; CreateSale runs inside its own
        /// transaction on this context, so it reads the categories itself.
        /// </summary>
        private async Task<HashSet<int>> SoldByLength(IEnumerable<int> productIds)
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
    }
}
