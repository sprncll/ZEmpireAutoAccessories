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
    }
}
