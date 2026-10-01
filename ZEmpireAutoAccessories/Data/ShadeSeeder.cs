using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Data
{
    /// <summary>
    /// The shades each nano ceramic tint variant comes in.
    ///
    /// These are in SeedPricelist2026.sql too, but a script only helps the
    /// machine somebody ran it on. Doing it at startup means a teammate who
    /// pulls the branch and restores an older database gets the same list
    /// without being told to run anything - the same reason the payment modes
    /// are seeded here.
    ///
    /// Insert-only, and matched on names. A shade already on a variant is left
    /// alone, and a shade nobody lists here is never removed: a job order or
    /// invoice line can point at one, and that line is how it says what went
    /// on the car.
    /// </summary>
    public static class ShadeSeeder
    {
        /// <summary>
        /// Variant name -> its shades, in the order they are offered.
        /// Matched against whichever product carries the variant, so the
        /// product names do not have to be repeated here.
        /// </summary>
        private static readonly (string Variant, string[] Shades)[] Catalogue =
        {
            // BF Film
            ("BF Stone",   new[] { "Light", "Medium", "Superdark" }),
            ("BF Pro",     new[] { "Light", "Medium", "Superdark" }),
            ("BF Supreme", new[] { "Light", "Medium", "Superdark" }),

            // Profilm
            ("Profilm X Pro/Advance", new[] { "Clear Blue", "Light", "Medium", "Dark", "Superdark" }),
            ("Profilm X Lite",        new[] { "Light", "Medium", "Superdark" })
        };

        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var wanted = Catalogue.Select(c => c.Variant).ToList();

            var variants = await context.TintVariants
                .Where(v => wanted.Contains(v.VariantName))
                .Include(v => v.Shades)
                .ToListAsync();

            var added = new List<string>();

            foreach (var (variantName, shades) in Catalogue)
            {
                // A variant name can be carried by more than one product; each
                // copy needs its own shades, because cat.Shade hangs off the
                // variant rather than the name.
                foreach (var variant in variants.Where(v =>
                             string.Equals(v.VariantName, variantName, StringComparison.OrdinalIgnoreCase)))
                {
                    var existing = new HashSet<string>(
                        variant.Shades.Select(s => s.ShadeName), StringComparer.OrdinalIgnoreCase);

                    foreach (var shade in shades.Where(s => !existing.Contains(s)))
                    {
                        variant.Shades.Add(new Shade { ShadeName = shade });
                        added.Add($"{variantName} / {shade}");
                    }
                }
            }

            var missing = wanted
                .Where(name => !variants.Any(v =>
                    string.Equals(v.VariantName, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (added.Count > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Added tint shade(s): {Shades}.", string.Join(", ", added));
            }

            // Said rather than silently skipped: a variant nobody has is a
            // product that was never seeded, and its shades will not appear.
            if (missing.Count > 0)
                logger.LogWarning(
                    "No tint variant named {Variants} exists, so its shades were not added. " +
                    "Seed the product first.", string.Join(", ", missing));
        }
    }
}
