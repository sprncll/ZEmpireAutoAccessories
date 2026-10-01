using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Data
{
    /// <summary>
    /// Makes sure every payment mode the shop offers exists as a row in
    /// sales.PaymentMode, so the dropdowns can offer it. Runs at startup
    /// beside IdentitySeeder.
    ///
    /// Three things, in order. A mode whose name has changed is renamed in
    /// place, keeping its id so the transactions on it follow. A mode the shop
    /// offers that is not in the table is added. And a mode the shop no longer
    /// offers is deleted - but only if nothing was ever taken on it.
    ///
    /// That last condition is the important one. sales.Sales and
    /// sales.ServiceInvoice point at these rows by a foreign key, and a mode
    /// with transactions on it is how those transactions say how they were
    /// paid. Removing it would be refused by the database, or, if it were not,
    /// would rewrite what somebody was handed at the counter. So it stays, and
    /// the log says why.
    /// </summary>
    public static class PaymentModeSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var rows = await context.PaymentModes.ToListAsync();

            // Renames first. "Card" becoming "Credit Cards" is the same mode
            // under a better name, so the row is renamed and keeps its id -
            // every sale and invoice already on it follows automatically.
            // Inserting the new name instead would leave two rows meaning one
            // thing, and the old one could not be deleted because those
            // transactions point at it.
            var renamed = new List<string>();

            foreach (var row in rows)
            {
                var to = PaymentModes.RenamedTo(row.PaymentModeName);
                if (to == null)
                    continue;

                // Unless something already carries the new name, in which case
                // renaming would make the pair this is trying to avoid. Leave
                // it; the old name simply stops being offered.
                if (rows.Any(other => other != row &&
                        string.Equals(other.PaymentModeName, to, StringComparison.OrdinalIgnoreCase)))
                    continue;

                renamed.Add($"{row.PaymentModeName} -> {to}");
                row.PaymentModeName = to;
            }

            if (renamed.Count > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Renamed payment mode(s): {Renamed}.", string.Join(", ", renamed));
            }

            var known = new HashSet<string>(
                rows.Select(r => r.PaymentModeName), StringComparer.OrdinalIgnoreCase);

            var missing = PaymentModes.OfferedNames
                .Where(name => !known.Contains(name))
                .ToList();

            if (missing.Count == 0)
            {
                await Prune(context, rows, logger);
                return;
            }

            foreach (var name in missing)
                context.PaymentModes.Add(new PaymentMode { PaymentModeName = name });

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Added payment mode(s): {Modes}.", string.Join(", ", missing));

            await Prune(context, rows, logger);
        }

        /// <summary>
        /// Deletes the modes the shop no longer offers - but only the ones
        /// nothing was ever taken on.
        ///
        /// sales.Sales and sales.ServiceInvoice are the only two things that
        /// point at a payment mode, and both by a foreign key that would
        /// refuse the delete anyway. A mode with transactions on it is how
        /// those transactions say how they were paid; removing it would
        /// either be refused or rewrite what somebody was actually handed at
        /// the counter. Those are left, and said out loud, so it is clear
        /// which ones went and which could not.
        /// </summary>
        private static async Task Prune(
            ApplicationDbContext context, List<PaymentMode> rows, ILogger logger)
        {
            var offered = new HashSet<string>(PaymentModes.OfferedNames, StringComparer.OrdinalIgnoreCase);

            var retired = rows
                .Where(r => !offered.Contains(r.PaymentModeName))
                .ToList();

            if (retired.Count == 0)
                return;

            var removed = new List<string>();
            var kept = new List<string>();

            foreach (var mode in retired)
            {
                var used = await context.Sales.AnyAsync(s => s.PaymentModeID == mode.PaymentModeID)
                    || await context.ServiceInvoices.AnyAsync(i => i.PaymentModeID == mode.PaymentModeID);

                if (used)
                {
                    kept.Add(mode.PaymentModeName);
                    continue;
                }

                context.PaymentModes.Remove(mode);
                removed.Add(mode.PaymentModeName);
            }

            if (removed.Count > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation(
                    "Removed unused payment mode(s): {Modes}.", string.Join(", ", removed));
            }

            if (kept.Count > 0)
                logger.LogWarning(
                    "Payment mode(s) {Modes} are no longer offered but still carry transactions, " +
                    "so they were kept. Move those transactions to a mode you do offer if you " +
                    "want the rows gone.", string.Join(", ", kept));
        }
    }
}
