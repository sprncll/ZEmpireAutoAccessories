using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Marks what has run out of time: quotations past their Valid Until date
    /// and warranties past their end date.
    ///
    /// Warranties used to expire only when somebody opened the Warranty list
    /// or that warranty - so the dashboard's Active Warranties counted covers
    /// that had ended months ago. Quotations never expired at all: a price
    /// quoted for seven days stayed a live Draft for ever.
    ///
    /// This runs from <see cref="ExpirySweeper"/> when the site starts and
    /// every few hours after, and again from the screens that show these
    /// statuses, so a figure is right whichever is looked at first.
    /// </summary>
    public class ExpiryService
    {
        private readonly ApplicationDbContext _context;

        public ExpiryService(ApplicationDbContext context) => _context = context;

        /// <summary>How many of each it marked.</summary>
        public async Task<(int Quotations, int Warranties)> Sweep()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // A quotation that became a job order is done with, not expired;
            // one already cancelled stays cancelled. Only a live one runs out.
            // Loaded and changed one by one rather than in a single UPDATE, so
            // each change is seen and written down like any other.
            var quotes = await _context.Quotations
                .Where(q => q.ValidUntil != null && q.ValidUntil < today
                            && q.JobOrder == null
                            && q.Status != "Cancelled" && q.Status != "Expired")
                .ToListAsync();

            foreach (var quote in quotes)
                quote.Status = "Expired";

            var warranties = await _context.Warranties
                .Where(w => w.WarrantyStatus == "Active"
                            && w.WarrantyEndDate != null && w.WarrantyEndDate < today)
                .ToListAsync();

            foreach (var warranty in warranties)
                warranty.WarrantyStatus = "Expired";

            if (quotes.Count + warranties.Count > 0)
                await _context.SaveChangesAsync();

            return (quotes.Count, warranties.Count);
        }
    }

    /// <summary>
    /// Runs <see cref="ExpiryService"/> when the site starts and every six
    /// hours while it is up. On shared hosting the site is stopped when idle
    /// and started by the next visitor, so "when the site starts" is most
    /// mornings - and the screens sweep again on their own in between.
    /// </summary>
    public class ExpirySweeper : BackgroundService
    {
        private static readonly TimeSpan Every = TimeSpan.FromHours(6);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<ExpirySweeper> _log;

        public ExpirySweeper(IServiceScopeFactory scopes, ILogger<ExpirySweeper> log)
        {
            _scopes = scopes;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stopping)
        {
            // Let the site finish starting before touching the database.
            try { await Task.Delay(TimeSpan.FromSeconds(20), stopping); }
            catch (OperationCanceledException) { return; }

            while (!stopping.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var (quotes, warranties) = await scope.ServiceProvider
                        .GetRequiredService<ExpiryService>().Sweep();

                    if (quotes + warranties > 0)
                        _log.LogInformation("Expired {Quotations} quotation(s) and {Warranties} warranty(ies).",
                            quotes, warranties);
                }
                catch (Exception ex)
                {
                    // A database that is down this morning is not a reason
                    // for the site to stop. The screens sweep as well.
                    _log.LogWarning(ex, "The expiry sweep could not run; it will try again later.");
                }

                try { await Task.Delay(Every, stopping); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
