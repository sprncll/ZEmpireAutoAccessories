namespace ZEmpireAutoAccessories.Authorization
{
    /// <summary>Custom claim types used by the RBAC layer.</summary>
    public static class AppClaims
    {
        /// <summary>
        /// One claim per accessible module name (from sec.RolePermission).
        /// Deliberately distinct from the database's existing "Permission"
        /// role claims (fine-grained actions such as "Reports.View"), because
        /// .NET compares claim types case-insensitively.
        /// </summary>
        public const string ModuleAccess = "module_access";

        /// <summary>The user's display name (asp.AspNetUsers.FullName).</summary>
        public const string FullName = "full_name";

        /// <summary>
        /// Modules only an Admin may hold, whatever sec.RolePermission says.
        /// The claim is withheld from everyone else, so the module disappears
        /// from the menu and ModuleAuthorize turns away a typed URL, without
        /// either having to know the rule.
        ///
        /// Services is here because its screens set what the shop charges for
        /// its own work. Staff still pick services on a job order, quotation or
        /// invoice - that reads the table, it does not open the module.
        /// </summary>
        public static readonly IReadOnlySet<string> AdminOnlyModules =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Services" };
    }
}
