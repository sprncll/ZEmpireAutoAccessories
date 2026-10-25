using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Data
{
    /// <summary>
    /// The panels a tint job can be priced and recorded against.
    ///
    /// Insert-only and matched on name, for the same reason as the shades: a
    /// pricing row, a document line and a vehicle checklist detail can all
    /// point at a panel, and those rows are how the system says what was
    /// fitted and what the car arrived in.
    ///
    /// Only the ones the shop was missing are listed. Everything already in
    /// cat.Panel is left exactly as it is.
    /// </summary>
    public static class PanelSeeder
    {
        /// <summary>
        /// Sunroof was the one piece of glass the shop fits that the panel
        /// list had no row for, so it could not be put on a job order at all.
        ///
        /// The measurement side already knew about it: SuggestedCutSizes
        /// carries sunroof glass for Subcompact/Crossover, SUV, MPV and Big
        /// SUV, and leaves it null for the classes that do not have one. So
        /// the moment this panel is priced, the film comes off the roll by
        /// the right length without anything further being configured.
        ///
        /// It is also deliberately NOT part of Full Wrap, which is the four
        /// main panels added up. A sunroof is priced on its own, and adding
        /// it to the wrap would deduct it twice.
        /// </summary>
        private static readonly string[] Panels = { "Sunroof" };

        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existing = new HashSet<string>(
                await context.Panels.Select(p => p.PanelName).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            var added = Panels.Where(p => !existing.Contains(p)).ToList();

            if (added.Count == 0)
                return;

            foreach (var name in added)
                context.Panels.Add(new Panel { PanelName = name });

            await context.SaveChangesAsync();

            // Said rather than left to be discovered: the add-line forms build
            // their panel list from cat.Pricing, so a panel with no price does
            // not appear on a document yet. The row has to exist before a
            // price can be set against it, which is what this seeder is for.
            logger.LogInformation(
                "Added panel(s): {Panels}. Set a price for each under Pricing " +
                "before they appear on the quotation, job order and invoice forms.",
                string.Join(", ", added));
        }
    }
}
