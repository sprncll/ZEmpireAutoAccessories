using System.Security.Claims;
using ZEmpireAutoAccessories.Authorization;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// One screen in the side navigation.
    /// </summary>
    /// <param name="Label">What the link says.</param>
    /// <param name="Controller">Where it goes (always the Index action).</param>
    /// <param name="Icon">Inner SVG markup, 24x24, stroke = currentColor.</param>
    /// <param name="Modules">
    /// The module claims that open it - holding ANY one is enough, which is
    /// the same rule [ModuleAuthorize] applies on the controller.
    /// </param>
    /// <param name="AlsoActiveFor">
    /// Other controllers that belong to this screen, so the link stays lit
    /// while you are on one of them (Product Categories under Products).
    /// </param>
    /// <param name="AdminOnly">Shown to the Admin role only, whatever the claims say.</param>
    public sealed record NavItem(
        string Label,
        string Controller,
        string Icon,
        string[] Modules,
        string[]? AlsoActiveFor = null,
        bool AdminOnly = false)
    {
        public bool Owns(string? controller) =>
            controller is not null &&
            (string.Equals(Controller, controller, StringComparison.OrdinalIgnoreCase) ||
             (AlsoActiveFor?.Contains(controller, StringComparer.OrdinalIgnoreCase) ?? false));
    }

    public sealed record NavGroup(string Name, IReadOnlyList<NavItem> Items);

    /// <summary>
    /// The side navigation: which screens exist, how they are grouped, and
    /// which of them a given user may open.
    ///
    /// The groups are the same ones the dashboard sorts its tiles into, so
    /// the menu and the dashboard describe the system the same way. A link
    /// is only offered when the controller behind it would let the user in,
    /// so nobody is shown a door that answers with Access Denied.
    /// </summary>
    public static class AppNav
    {
        public const string DashboardIcon =
            "<rect width='7' height='9' x='3' y='3' rx='1'/><rect width='7' height='5' x='14' y='3' rx='1'/><rect width='7' height='9' x='14' y='12' rx='1'/><rect width='7' height='5' x='3' y='16' rx='1'/>";

        private const string ClipboardList =
            "<rect width='8' height='4' x='8' y='2' rx='1' ry='1'/><path d='M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2'/><path d='M12 11h4'/><path d='M12 16h4'/><path d='M8 11h.01'/><path d='M8 16h.01'/>";

        private static readonly NavGroup[] Groups =
        {
            new("Overview", new NavItem[]
            {
                new("Reports", "Report",
                    "<line x1='18' y1='20' x2='18' y2='10'/><line x1='12' y1='20' x2='12' y2='4'/><line x1='6' y1='20' x2='6' y2='14'/>",
                    new[] { "Reports" }),
            }),

            new("Customers & Vehicles", new NavItem[]
            {
                new("Customers", "Customer",
                    "<path d='M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M22 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/>",
                    new[] { "Customers" }),
                new("Vehicles", "Vehicle",
                    "<path d='M14 16H9m10 0h3v-3.15a1 1 0 0 0-.84-.99L16 11l-2.7-3.6a1 1 0 0 0-.8-.4H5.24a2 2 0 0 0-1.8 1.1l-.8 1.63A6 6 0 0 0 2 12.42V16h2'/><circle cx='6.5' cy='16.5' r='2.5'/><circle cx='16.5' cy='16.5' r='2.5'/>",
                    new[] { "Vehicles" },
                    AlsoActiveFor: new[] { "VehicleClassification" }),
                new("Warranty", "Warranty",
                    "<path d='M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z'/><path d='m9 12 2 2 4-4'/>",
                    new[] { "Warranty" }),
            }),

            new("Operations", new NavItem[]
            {
                new("Job Orders", "JobOrder", ClipboardList, new[] { "Job Orders" }),
                new("Vehicle Checklist", "VehicleChecklist",
                    "<rect width='8' height='4' x='8' y='2' rx='1' ry='1'/><path d='M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2'/><path d='m9 14 2 2 4-4'/>",
                    new[] { "Vehicle Checklist" }),
                new("Services", "Service",
                    "<path d='M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z'/>",
                    new[] { "Services" }),
                new("Job Types", "JobType",
                    "<path d='M9 5H2v7l6.29 6.29c.94.94 2.48.94 3.42 0l3.58-3.58c.94-.94.94-2.48 0-3.42L9 5Z'/><path d='M6 9.01V9'/><path d='m15 5 6.3 6.3a2.4 2.4 0 0 1 0 3.4L17 19'/>",
                    new[] { "Job Types" }),
            }),

            new("Catalog & Inventory", new NavItem[]
            {
                new("Products", "Product",
                    "<path d='M16.5 9.4 7.5 4.21'/><path d='M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z'/><path d='M3.27 6.96 12 12.01l8.73-5.05'/><path d='M12 22.08V12'/>",
                    new[] { "Products" },
                    AlsoActiveFor: new[] { "ProductCategory" }),
                new("Inventory", "Inventory",
                    "<rect width='20' height='5' x='2' y='3' rx='1'/><path d='M4 8v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V8'/><path d='M10 12h4'/>",
                    new[] { "Inventory" }),
                // Stock counts and cut sizes share the Inventory module rather
                // than owning a sec.Module row of their own.
                new("Stock Counts", "InventoryCheck",
                    "<path d='M11 6h10'/><path d='M11 12h10'/><path d='M11 18h10'/><path d='m3 6 1.5 1.5L7 5'/><path d='m3 12 1.5 1.5L7 11'/><path d='m3 18 1.5 1.5L7 17'/>",
                    new[] { "Inventory" }),
                new("Cut Sizes", "CutSize",
                    "<circle cx='6' cy='6' r='3'/><path d='M8.12 8.12 12 12'/><path d='M20 4 8.12 15.88'/><circle cx='6' cy='18' r='3'/><path d='M14.8 14.8 20 20'/>",
                    new[] { "Inventory" }),
                new("Pricing", "Pricing",
                    "<circle cx='12' cy='12' r='10'/><path d='M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8'/><path d='M12 18V6'/>",
                    new[] { "Pricing" }),
                new("Price History", "PriceHistory",
                    "<path d='M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8'/><path d='M3 3v5h5'/><path d='M12 7v5l4 2'/>",
                    new[] { "Price History" }),
            }),

            new("Sales & Billing", new NavItem[]
            {
                new("Quotation", "Quotation",
                    "<path d='M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z'/><path d='M14 2v4a2 2 0 0 0 2 2h4'/><path d='M16 13H8'/><path d='M16 17H8'/><path d='M10 9H8'/>",
                    new[] { "Quotation" }),
                new("Sales", "Sales",
                    "<circle cx='8' cy='21' r='1'/><circle cx='19' cy='21' r='1'/><path d='M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12'/>",
                    new[] { "Sales" }),
                new("Service Invoices", "ServiceInvoice",
                    "<path d='M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1-2 1Z'/><path d='M16 8h-6a2 2 0 1 0 0 4h4a2 2 0 1 1 0 4H8'/><path d='M12 17.5v-11'/>",
                    new[] { "Service Invoices" }),
                // Cashiering needs no row in sec.Module: it only combines Sales
                // and Service Invoices, so holding either one opens it. That is
                // how CashieringController is gated too.
                new("Cashiering", "Cashiering",
                    "<rect width='20' height='12' x='2' y='6' rx='2'/><circle cx='12' cy='12' r='2'/><path d='M6 12h.01'/><path d='M18 12h.01'/>",
                    new[] { "Cashiering", "Sales", "Service Invoices" }),
            }),

            new("Administration", new NavItem[]
            {
                new("User Management", "Employee",
                    "<circle cx='9' cy='7' r='4'/><path d='M10.5 15H6a4 4 0 0 0-4 4v2h8.5'/><circle cx='18' cy='16' r='2.5'/><path d='M18 13v1'/><path d='M18 18v1'/><path d='M21 16h-1'/><path d='M16 16h-1'/>",
                    new[] { "User Management" }),
                // SecurityController is [Authorize(Roles = "Admin")], so the
                // role decides this one, not a module claim.
                new("Roles & Permissions", "Security",
                    "<path d='M2.586 17.414A2 2 0 0 0 2 18.828V21a1 1 0 0 0 1 1h3a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h1a1 1 0 0 0 1-1v-1a1 1 0 0 1 1-1h.172a2 2 0 0 0 1.414-.586l.814-.814a6.5 6.5 0 1 0-4-4z'/><circle cx='16.5' cy='7.5' r='.5' fill='currentColor'/>",
                    new[] { "Roles" },
                    AdminOnly: true),
            }),
        };

        // Controllers with a Details screen, so the top bar's Back from an
        // Edit can return to the record rather than to the whole list.
        private static readonly HashSet<string> WithDetails = new(StringComparer.OrdinalIgnoreCase)
        {
            "Customer", "Employee", "InventoryCheck", "Inventory", "JobOrder", "Pricing", "Product",
            "Quotation", "Sales", "Service", "ServiceInvoice", "VehicleChecklist",
            "VehicleClassification", "Vehicle", "Warranty",
        };

        public static bool HasDetails(string controller) => WithDetails.Contains(controller);

        // One record of each module, for "Back to job order" rather than
        // "Back to job orders" when Back returns to a single record.
        private static readonly Dictionary<string, string> OneOf = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Customer"] = "customer", ["Vehicle"] = "vehicle", ["Warranty"] = "warranty",
            ["JobOrder"] = "job order", ["VehicleChecklist"] = "checklist", ["Service"] = "service",
            ["JobType"] = "job type", ["Product"] = "product", ["Inventory"] = "stock item",
            ["InventoryCheck"] = "stock count", ["Pricing"] = "price", ["Quotation"] = "quotation",
            ["Sales"] = "sale", ["ServiceInvoice"] = "invoice", ["Employee"] = "user",
            ["VehicleClassification"] = "vehicle class", ["ProductCategory"] = "category",
        };

        /// <summary>
        /// What a Back button pointing at <paramref name="url"/> should say:
        /// "Back to Cashiering", "Back to job order", "Back to Dashboard".
        /// Worked out from the address alone; anything unrecognised is "Back".
        /// </summary>
        public static string BackLabel(string url)
        {
            var path = url.Split('?', '#')[0].Trim('/');
            if (path.Length == 0)
                return "Back to Dashboard";

            var parts = path.Split('/');
            var controller = parts[0];
            var action = parts.Length > 1 ? parts[1] : "Index";

            if (controller.Equals("Home", StringComparison.OrdinalIgnoreCase))
                return "Back to Dashboard";

            if (Locate(controller) is not { } spot)
                return "Back";

            if (action.Equals("Index", StringComparison.OrdinalIgnoreCase))
                return "Back to " + spot.Item.Label;

            if (OneOf.TryGetValue(controller, out var one))
                return "Back to " + one;

            return "Back to " + spot.Item.Label;
        }

        /// <summary>
        /// The groups this user has at least one screen in, each holding only
        /// the screens they may open. Empty groups are left out.
        /// </summary>
        public static IReadOnlyList<NavGroup> For(ClaimsPrincipal user)
        {
            var held = new HashSet<string>(
                user.FindAll(AppClaims.ModuleAccess).Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);
            var isAdmin = user.IsInRole("Admin");

            var visible = new List<NavGroup>();
            foreach (var group in Groups)
            {
                var items = group.Items
                    .Where(i => i.AdminOnly ? isAdmin : i.Modules.Any(held.Contains))
                    .ToList();
                if (items.Count > 0) { visible.Add(new NavGroup(group.Name, items)); }
            }
            return visible;
        }

        /// <summary>
        /// The screen a controller belongs to and the group it sits in, for
        /// the "where am I" line in the top bar. Null for screens outside the
        /// menu (the dashboard, Change Password).
        /// </summary>
        public static (NavGroup Group, NavItem Item)? Locate(string? controller)
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    if (item.Owns(controller)) { return (group, item); }
                }
            }
            return null;
        }
    }
}
