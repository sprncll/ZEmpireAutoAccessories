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
            // RemoveLine takes a line item off a quotation, job order or
            // invoice. It does not begin with "Delete" but it deletes a row
            // all the same, so it is held to the same rule. (Photos and proof
            // files are deliberately not: swapping a wrong file is part of
            // filling the form in.)
            var isDelete = action.ActionName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
                || action.ActionName.Equals("RemoveLine", StringComparison.OrdinalIgnoreCase);
            if (!isDelete)
                return;

            action.Filters.Add(AdminOnly);
        }
    }
}
