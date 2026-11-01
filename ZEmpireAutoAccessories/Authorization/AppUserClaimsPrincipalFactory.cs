using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Authorization
{
    /// <summary>
    /// Extends the standard Identity principal (which already carries role
    /// claims) with the user's display name and one permission claim per
    /// accessible module, so authorization and menus can be driven from the
    /// cookie without hitting the database on every request.
    /// </summary>
    public class AppUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        private readonly IPermissionService _permissions;

        public AppUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> options,
            IPermissionService permissions)
            : base(userManager, roleManager, options)
        {
            _permissions = permissions;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            identity.AddClaim(new Claim(AppClaims.FullName,
                string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? string.Empty) : user.FullName));

            foreach (var module in await _permissions.GetModulesForUserAsync(user.Id))
                identity.AddClaim(new Claim(AppClaims.ModuleAccess, module));

            return identity;
        }
    }
}
