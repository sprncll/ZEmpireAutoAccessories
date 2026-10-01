using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<CollectedTotal> GetDailySales() =>
            Collected(DateTime.Today, DateTime.Today.AddDays(1));

        public Task<CollectedTotal> GetWeeklySales()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            return Collected(startOfWeek, startOfWeek.AddDays(7));
        }

        public Task<CollectedTotal> GetMonthlySales()
        {
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return Collected(startOfMonth, startOfMonth.AddMonths(1));
        }

        /// <summary>
        /// What was taken between two instants: product sales plus the service
        /// invoices actually paid.
        ///
        /// These figures used to count sales.Sales alone, which left every
        /// service invoice off the front page of the reports - in a shop whose
        /// work is mostly service, the headline number was a fraction of the
        /// takings and a P42,000 tint job did not move it at all.
        ///
        /// A service invoice counts only once it is Paid, which is the rule
        /// Cashiering and the Collections report use, so the three agree.
        ///
        /// It is dated by InvoiceDate, not by when the money came in, because
        /// there is nowhere to record the latter - sales.ServiceInvoice has no
        /// paid-at column and adding one is off the table. So an invoice
        /// raised yesterday and paid today lands on yesterday, and yesterday's
        /// figure moves when it is paid. Cashiering ranges the same way, which
        /// is what keeps the two agreeing; the alternative would be a headline
        /// figure that disagrees with the screen it links to.
        /// </summary>
        private async Task<CollectedTotal> Collected(DateTime from, DateTime toExclusive)
        {
            return new CollectedTotal
            {
                Sales = await _context.Sales
                    .Where(s => s.SalesDate >= from && s.SalesDate < toExclusive)
                    .SumAsync(s => (decimal?)s.TotalAmount) ?? 0,

                ServiceInvoices = await _context.ServiceInvoices
                    .Where(i => i.InvoiceDate >= from && i.InvoiceDate < toExclusive
                                && i.Status == "Paid")
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0
            };
        }

        public async Task<List<VwStockOnHand>> GetLowStock(decimal threshold = 5)
        {
            return await _context.StockOnHand
                .Where(s => (s.StockOnHand ?? 0) <= threshold)
                .OrderBy(s => s.StockOnHand)
                .ToListAsync();
        }

        public async Task<List<Sale>> GetRecentSales(int count = 10)
        {
            return await _context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SalesDate)
                .Take(count)
                .ToListAsync();
        }
    }
}
