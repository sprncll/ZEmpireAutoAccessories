using Microsoft.AspNetCore.Authorization;

namespace ZEmpireAutoAccessories.Authorization
{
    /// <summary>
    /// Restricts an action to the Admin role. Module access (see
    /// <see cref="ModuleAuthorizeAttribute"/>) says which screens a role can
    /// reach at all; this says which actions inside those screens only an Admin
    /// may run. Staff keep the module but lose the action.
    ///
    /// Deleting is covered everywhere by <see cref="AdminOnlyDeleteConvention"/>
    /// rather than by this attribute, so a new Delete action cannot be added
    /// without the restriction. Put this on anything else that is Admin's
    /// alone - adding a product, for instance.
    /// </summary>
    public sealed class AdminOnlyAttribute : AuthorizeAttribute
    {
        public const string AdminRole = "Admin";

        public AdminOnlyAttribute() => Roles = AdminRole;
    }
}
