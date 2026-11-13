using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Whether the tables added by a script in Database/ are there yet.
    ///
    /// Tables are added to this system by running a script, by hand, against
    /// each database - there are no migrations. So for a while after a pull,
    /// the code knows about a table the database does not have, and a query
    /// against it fails with "Invalid object name" on every screen that reads
    /// it. Asking first lets a screen carry on the old way until the script
    /// is run, and say so, rather than turning into an error page.
    ///
    /// The answer is kept for a couple of minutes: long enough that this is
    /// not a query per request, short enough that running the script takes
    /// effect without restarting the site.
    /// </summary>
    public class SchemaStatus
    {
        public const string Payments = "sales.ServiceInvoicePayment";
        public const string ActivityLog = "sec.ActivityLog";

        private static readonly TimeSpan Remember = TimeSpan.FromMinutes(2);

        private readonly Dictionary<string, (bool Present, DateTime CheckedAt)> _known = new();
        private readonly object _lock = new();

        public async Task<bool> Has(ApplicationDbContext db, string table)
        {
            // Anything but SQL Server - the in-memory store a test runs on -
            // has every table the model names.
            if (!db.Database.IsRelational())
                return true;

            lock (_lock)
            {
                if (_known.TryGetValue(table, out var known) && DateTime.UtcNow - known.CheckedAt < Remember)
                    return known.Present;
            }

            bool present;
            try
            {
                present = await db.Database
                    .SqlQuery<int>($"SELECT CASE WHEN OBJECT_ID({table}, N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
                    .SingleAsync() == 1;
            }
            catch
            {
                // If the question cannot be asked, assume the answer is no:
                // the old way of working is the one known to run.
                present = false;
            }

            lock (_lock)
                _known[table] = (present, DateTime.UtcNow);

            return present;
        }

        /// <summary>Forget what was learned, so the next ask goes to the database.</summary>
        public void Forget()
        {
            lock (_lock)
                _known.Clear();
        }
    }
}
