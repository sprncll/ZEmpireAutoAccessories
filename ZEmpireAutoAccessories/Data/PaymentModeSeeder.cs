using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Data
{
    /// <summary>
    /// Makes sure every payment mode the shop offers exists as a row in
    /// sales.PaymentMode, so the dropdowns can offer it. Runs at startup
    /// beside IdentitySeeder.
    ///
    /// It only ever inserts. A mode already in the table is left exactly as it
    /// is, and one that is no longer offered - GCash, Cheque - is never
    /// removed or renamed: sales.Sales and sales.ServiceInvoice point at these
    /// rows, so deleting one would orphan every transaction that used it and
    /// rewrite history besides. Retiring a mode is a matter of leaving it off
    /// the offered list, which keeps it out of the forms while the old
    /// transactions keep reading correctly.
    /// </summary>
    public static class PaymentModeSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existing = await context.PaymentModes
                .Select(p => p.PaymentModeName)
                .ToListAsync();

            var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

            var missing = PaymentModes.OfferedNames
                .Where(name => !known.Contains(name))
                .ToList();

            if (missing.Count == 0)
                return;

            foreach (var name in missing)
                context.PaymentModes.Add(new PaymentMode { PaymentModeName = name });

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Added payment mode(s): {Modes}.", string.Join(", ", missing));
        }
    }
}
