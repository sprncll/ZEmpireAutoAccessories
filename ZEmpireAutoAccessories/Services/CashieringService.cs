using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Read-only cashier view over the two things the shop collects money
    /// for: product sales (sales.Sales) and service invoices
    /// (sales.ServiceInvoice). The two tables have different shapes, so each
    /// is filtered and projected onto CashierTransaction in SQL and the two
    /// result sets are merged in memory - a UNION would have to be hand
    /// written, and the filtered day range keeps the row count small.
    /// </summary>
    public class CashieringService : ICashieringService
    {
        private readonly ApplicationDbContext _context;
        private readonly SchemaStatus _schema;

        public CashieringService(ApplicationDbContext context, SchemaStatus schema)
        {
            _context = context;
            _schema = schema;
        }

        public async Task<CashieringViewModel> GetCashiering(
            DateOnly dateFrom,
            DateOnly dateTo,
            string? q = null,
            int? paymentModeId = null,
            string? userId = null,
            CashierSource? source = null)
        {
            // Guard against an inverted range rather than silently returning
            // nothing - the date pickers let either end be typed freely.
            if (dateTo < dateFrom)
                (dateFrom, dateTo) = (dateTo, dateFrom);

            var from = dateFrom.ToDateTime(TimeOnly.MinValue);
            var toExclusive = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var term = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            var transactions = new List<CashierTransaction>();

            if (source is null or CashierSource.Sale)
                transactions.AddRange(await GetSaleRows(from, toExclusive, term, paymentModeId, userId));

            if (source is null or CashierSource.ServiceInvoice)
                transactions.AddRange(await _schema.Has(_context, SchemaStatus.Payments)
                    ? await GetInvoicePaymentRows(from, toExclusive, term, paymentModeId, userId)
                    : await GetServiceInvoiceRows(from, toExclusive, term, paymentModeId, userId));

            transactions = transactions
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.InvoiceNumber)
                .ToList();

            var model = BuildViewModel(transactions, dateFrom, dateTo, term, paymentModeId, userId, source);
            model.UsesLedger = await _schema.Has(_context, SchemaStatus.Payments);
            return model;
        }

        private async Task<List<CashierTransaction>> GetSaleRows(
            DateTime from, DateTime toExclusive, string? term, int? paymentModeId, string? userId)
        {
            var query = _context.Sales
                .Where(s => s.SalesDate >= from && s.SalesDate < toExclusive);

            if (term != null)
                query = query.Where(s => s.InvoiceNumber.Contains(term) || s.Customer.FullName.Contains(term));

            if (paymentModeId.HasValue)
                query = query.Where(s => s.PaymentModeID == paymentModeId.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(s => s.UserId == userId);

            return await query
                .Select(s => new CashierTransaction
                {
                    Source = CashierSource.Sale,
                    SourceId = s.SalesID,
                    InvoiceNumber = s.InvoiceNumber,
                    TransactionDate = s.SalesDate,
                    CustomerName = s.Customer.FullName,
                    PlateNumber = s.Vehicle != null ? s.Vehicle.PlateNumber : null,
                    PaymentModeID = s.PaymentModeID,
                    PaymentModeName = s.PaymentMode.PaymentModeName,
                    UserId = s.UserId,
                    CashierName = s.User.FullName != null && s.User.FullName != ""
                        ? s.User.FullName
                        : s.User.UserName,
                    TotalAmount = s.TotalAmount,
                    // sales.Sales has no AmountPaid/ChangeAmount columns.
                    AmountPaid = null,
                    ChangeAmount = null,
                    // ...and no Status column either: recording a product
                    // sale is the act of collecting for it.
                    Status = "Paid"
                })
                .ToListAsync();
        }

        private async Task<List<CashierTransaction>> GetServiceInvoiceRows(
            DateTime from, DateTime toExclusive, string? term, int? paymentModeId, string? userId)
        {
            var query = _context.ServiceInvoices
                .Where(i => i.InvoiceDate >= from && i.InvoiceDate < toExclusive);

            if (term != null)
                query = query.Where(i => i.InvoiceNumber.Contains(term) || i.Customer.FullName.Contains(term));

            if (paymentModeId.HasValue)
                query = query.Where(i => i.PaymentModeID == paymentModeId.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(i => i.UserId == userId);

            return await query
                .Select(i => new CashierTransaction
                {
                    Source = CashierSource.ServiceInvoice,
                    SourceId = i.ServiceInvoiceID,
                    InvoiceNumber = i.InvoiceNumber,
                    TransactionDate = i.InvoiceDate,
                    CustomerName = i.Customer.FullName,
                    PlateNumber = i.Vehicle != null ? i.Vehicle.PlateNumber : null,
                    PaymentModeID = i.PaymentModeID,
                    PaymentModeName = i.PaymentMode.PaymentModeName,
                    UserId = i.UserId,
                    CashierName = i.User.FullName != null && i.User.FullName != ""
                        ? i.User.FullName
                        : i.User.UserName,
                    TotalAmount = i.TotalAmount,
                    AmountPaid = i.AmountPaid,
                    ChangeAmount = i.ChangeAmount,
                    Status = i.Status
                })
                .ToListAsync();
        }

        /// <summary>
        /// Service invoice rows from the payment ledger: every payment taken in
        /// the range, on the day it was taken - a down payment today is in
        /// today's takings, whenever the invoice was raised - and every invoice
        /// raised in the range that is still owed something, as the balance
        /// due, which is not money in.
        /// </summary>
        private async Task<List<CashierTransaction>> GetInvoicePaymentRows(
            DateTime from, DateTime toExclusive, string? term, int? paymentModeId, string? userId)
        {
            var payments = _context.ServiceInvoicePayments
                .Where(p => p.PaymentDate >= from && p.PaymentDate < toExclusive);

            if (term != null)
                payments = payments.Where(p => p.ServiceInvoice.InvoiceNumber.Contains(term)
                                            || p.ServiceInvoice.Customer.FullName.Contains(term));

            if (paymentModeId.HasValue)
                payments = payments.Where(p => p.PaymentModeID == paymentModeId.Value);

            if (!string.IsNullOrEmpty(userId))
                payments = payments.Where(p => p.UserId == userId);

            var rows = await payments
                .Select(p => new CashierTransaction
                {
                    FromLedger = true,
                    PaymentId = p.PaymentID,
                    Source = CashierSource.ServiceInvoice,
                    SourceId = p.ServiceInvoiceID,
                    InvoiceNumber = p.ServiceInvoice.InvoiceNumber,
                    TransactionDate = p.PaymentDate,
                    CustomerName = p.ServiceInvoice.Customer.FullName,
                    PlateNumber = p.ServiceInvoice.Vehicle != null ? p.ServiceInvoice.Vehicle.PlateNumber : null,
                    PaymentModeID = p.PaymentModeID,
                    PaymentModeName = p.PaymentMode.PaymentModeName,
                    UserId = p.UserId,
                    TotalAmount = p.Amount,
                    DocumentTotal = p.ServiceInvoice.TotalAmount,
                    AmountPaid = p.AmountTendered,
                    ChangeAmount = p.AmountTendered != null ? p.ChangeAmount : null,
                    Status = p.ServiceInvoice.Status,
                    PaymentLabel = p.Notes ?? p.Kind
                })
                .ToListAsync();

            // Raised in the range and not settled: what is still owed, or the
            // whole of a cancelled or refunded one, shown so the cashier can
            // see it but never counted as money in.
            var owed = _context.ServiceInvoices
                .Where(i => i.InvoiceDate >= from && i.InvoiceDate < toExclusive && i.Status != "Paid")
                .Where(i => i.Status != "Pending" || i.AmountPaid < i.TotalAmount);

            if (term != null)
                owed = owed.Where(i => i.InvoiceNumber.Contains(term) || i.Customer.FullName.Contains(term));

            if (paymentModeId.HasValue)
                owed = owed.Where(i => i.PaymentModeID == paymentModeId.Value);

            if (!string.IsNullOrEmpty(userId))
                owed = owed.Where(i => i.UserId == userId);

            rows.AddRange(await owed
                .Select(i => new CashierTransaction
                {
                    FromLedger = true,
                    Source = CashierSource.ServiceInvoice,
                    SourceId = i.ServiceInvoiceID,
                    InvoiceNumber = i.InvoiceNumber,
                    TransactionDate = i.InvoiceDate,
                    CustomerName = i.Customer.FullName,
                    PlateNumber = i.Vehicle != null ? i.Vehicle.PlateNumber : null,
                    PaymentModeID = i.PaymentModeID,
                    PaymentModeName = i.PaymentMode.PaymentModeName,
                    UserId = i.UserId,
                    TotalAmount = i.Status == "Pending" ? i.TotalAmount - i.AmountPaid : i.TotalAmount,
                    DocumentTotal = i.TotalAmount,
                    Status = i.Status,
                    PaymentLabel = i.Status == "Pending" ? "Balance due" : i.Status
                })
                .ToListAsync());

            // Who took each payment. The ledger keeps the user's id only, so
            // the names are looked up once for the whole page.
            var ids = rows.Select(r => r.UserId).Distinct().ToList();
            var names = await _context.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.FullName != null && u.FullName != "" ? u.FullName : u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            foreach (var row in rows)
                row.CashierName = names.TryGetValue(row.UserId, out var name) ? name : null;

            return rows;
        }

        /// <summary>
        /// Drawer totals. Only collected rows (every product sale, plus
        /// service invoices that reached Paid) count towards the money in;
        /// Pending, Cancelled and Refunded invoices are reported separately
        /// so the cashier can see them without them inflating the total.
        /// </summary>
        private static CashieringViewModel BuildViewModel(
            List<CashierTransaction> transactions,
            DateOnly dateFrom,
            DateOnly dateTo,
            string? term,
            int? paymentModeId,
            string? userId,
            CashierSource? source)
        {
            var collected = transactions.Where(t => t.IsCollected).ToList();
            var uncollected = transactions.Where(t => !t.IsCollected).ToList();

            return new CashieringViewModel
            {
                Transactions = transactions,

                DateFrom = dateFrom,
                DateTo = dateTo,
                Search = term,
                PaymentModeID = paymentModeId,
                UserId = userId,
                Source = source,

                SalesTotal = collected
                    .Where(t => t.Source == CashierSource.Sale)
                    .Sum(t => t.TotalAmount),
                ServiceInvoiceTotal = collected
                    .Where(t => t.Source == CashierSource.ServiceInvoice)
                    .Sum(t => t.TotalAmount),
                GrandTotal = collected.Sum(t => t.TotalAmount),
                UncollectedTotal = uncollected.Sum(t => t.TotalAmount),

                CollectedCount = collected.Count,
                UncollectedCount = uncollected.Count,

                ByPaymentMode = collected
                    .GroupBy(t => new { t.PaymentModeID, t.PaymentModeName })
                    .Select(g => new CashierPaymentModeTotal
                    {
                        PaymentModeID = g.Key.PaymentModeID,
                        PaymentModeName = g.Key.PaymentModeName,
                        TransactionCount = g.Count(),
                        Total = g.Sum(t => t.TotalAmount)
                    })
                    .OrderByDescending(m => m.Total)
                    .ToList(),

                ByCashier = collected
                    .GroupBy(t => new { t.UserId, t.CashierName })
                    .Select(g => new CashierUserTotal
                    {
                        UserId = g.Key.UserId,
                        CashierName = g.Key.CashierName,
                        TransactionCount = g.Count(),
                        Total = g.Sum(t => t.TotalAmount)
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList()
            };
        }
    }
}
