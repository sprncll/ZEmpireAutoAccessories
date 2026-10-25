using Microsoft.AspNetCore.Identity;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Data
{
    /// <summary>
    /// Idempotent startup seeding: makes sure the RBAC roles exist as Identity
    /// roles, and gives a sign-in password to any user that was seeded via SQL
    /// without one (all existing users currently have PasswordHash = NULL).
    /// </summary>
    public static class IdentitySeeder
    {
        private static readonly string[] Roles = { "Admin", "Staff" };

        public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new ApplicationRole(role));
            }

            // One-time cleanup: this app only has Admin and Staff now. Remove
            // the old Manager role, but only if nobody's still assigned to
            // it - reassign those users to Admin or Staff first if this warns.
            var managerRole = await roleManager.FindByNameAsync("Manager");
            if (managerRole != null)
            {
                var managerUsers = await userManager.GetUsersInRoleAsync("Manager");
                if (managerUsers.Count == 0)
                {
                    await roleManager.DeleteAsync(managerRole);
                }
                else
                {
                    logger.LogWarning(
                        "The Manager role still has {Count} user(s) assigned ({Users}) and was not removed. " +
                        "Reassign them to Admin or Staff, then restart the app to finish removing it.",
                        managerUsers.Count, string.Join(", ", managerUsers.Select(u => u.UserName)));
                }
            }

            // A published site must not hand out a password that is printed in
            // the source. In Development the built-in one keeps a fresh clone
            // usable with nothing to configure; in Production an unset
            // Seed:DefaultPassword means the seeding is SKIPPED and said
            // loudly, rather than every passwordless account quietly becoming
            // reachable with a password anyone can read on GitHub.
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var configured = config["Seed:DefaultPassword"];

            if (string.IsNullOrWhiteSpace(configured) && environment.IsProduction())
            {
                logger.LogError(
                    "Seed:DefaultPassword is not set, so no passwords were seeded. " +
                    "Any account without one cannot sign in. Set it in the hosting " +
                    "control panel (or appsettings.Production.json) and restart.");
                return;
            }

            var defaultPassword = configured ?? "ChangeMe123!";
            var seeded = new List<string>();

            foreach (var user in userManager.Users.ToList())
            {
                if (await userManager.HasPasswordAsync(user))
                    continue;

                if (string.IsNullOrEmpty(user.SecurityStamp))
                    await userManager.UpdateSecurityStampAsync(user);

                var result = await userManager.AddPasswordAsync(user, defaultPassword);
                if (result.Succeeded)
                    seeded.Add(user.UserName ?? user.Id);
                else
                    logger.LogWarning("Could not set default password for {User}: {Errors}",
                        user.UserName, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            if (seeded.Count > 0)
            {
                // The development default is already public, so naming it helps.
                // A configured one is a real secret and is never logged.
                if (configured == null)
                    logger.LogWarning(
                        "Seeded default password \"{Password}\" for {Count} user(s) that had none: {Users}. " +
                        "CHANGE THESE PASSWORDS before real use.",
                        defaultPassword, seeded.Count, string.Join(", ", seeded));
                else
                    logger.LogWarning(
                        "Seeded the configured default password for {Count} user(s) that had none: {Users}. " +
                        "CHANGE THESE PASSWORDS before real use.",
                        seeded.Count, string.Join(", ", seeded));
            }
        }
    }
}
