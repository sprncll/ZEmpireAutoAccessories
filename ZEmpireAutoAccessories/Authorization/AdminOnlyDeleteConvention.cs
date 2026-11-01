using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace ZEmpireAutoAccessories.Authorization
{
    /// <summary>
    /// Nothing in the system may be deleted by anyone but an Admin.
    ///
    /// Applied as a convention rather than as an attribute on each of the forty
    /// or so Delete actions, because the rule has to hold for the ones written
    /// next year too. Any action whose name begins with "Delete" is covered the
    /// moment it is written - including the POST halves, which carry
    /// [ActionName("Delete")], and SecurityController.DeleteModule.
    ///
    /// The views hide their Delete buttons from Staff as well, but that is only
    /// for the look of the thing: this is what actually stops a typed URL.
    /// </summary>
    public sealed class AdminOnlyDeleteConvention : IActionModelConvention
    {
        private static readonly AuthorizeFilter AdminOnly = new(
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin)
                .Build());

        public void Apply(ActionModel action)
        {
            if (!action.ActionName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                return;

            action.Filters.Add(AdminOnly);
        }
    }
}
