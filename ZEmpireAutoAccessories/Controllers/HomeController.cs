using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IReportService reportService)
        {
            _logger = logger;
            _context = context;
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            var modules = new HashSet<string>(
                User.FindAll(AppClaims.ModuleAccess).Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            // Each quick-stat only appears if the user actually has access to
            // the module it links to.
            var stats = new List<DashboardStat>();

            // Peso figures are Admin-only - Staff still gets the operational
            // counts below (job orders, stock, warranties), just not revenue.
            // Everything taken today, not product sales alone - most of this
            // shop's money comes in on service invoices, which this figure
            // used to leave out entirely.
            //
            // Which half is shown follows what the user holds, the same way
            // Cashiering narrows its rows. A Sales-only user seeing a figure
            // with invoice money in it, on a tile linking to a screen that
            // then hides those rows, is a number they cannot reconcile.
            var seesSales = modules.Contains("Sales");
            var seesInvoices = modules.Contains("Service Invoices");

            if ((seesSales || seesInvoices) && User.IsInRole("Admin"))
            {
                var today = await _reportService.GetDailySales();

                var amount = seesSales && seesInvoices ? today.Total
                    : seesSales ? today.Sales
                    : today.ServiceInvoices;

                var label = seesSales && seesInvoices ? "Collected Today"
                    : seesSales ? "Today's Sales"
                    : "Today's Service Invoices";

                stats.Add(new DashboardStat(label, "₱" + amount.ToString("N2"),
                    Url.Action("Index", "Cashiering")!, "sales"));
            }

            if (modules.Contains("Job Orders"))
            {
                var pending = await _context.JobOrders.CountAsync(j => j.Status == "Pending");
                stats.Add(new DashboardStat("Pending Job Orders", pending.ToString(), Url.Action("Index", "JobOrder", new { status = "Pending" })!, "jobs"));
            }

            if (modules.Contains("Inventory"))
            {
                var lowStock = (await _reportService.GetLowStock()).Count;
                stats.Add(new DashboardStat("Low Stock Items", lowStock.ToString(), Url.Action("Index", "Inventory", new { status = "LowStock" })!, "stock"));
            }

            if (modules.Contains("Warranty"))
            {
                var active = await _context.Warranties.CountAsync(w => w.WarrantyStatus == "Active");
                stats.Add(new DashboardStat("Active Warranties", active.ToString(), Url.Action("Index", "Warranty", new { status = "Active" })!, "warranty"));
            }

            ViewData["Stats"] = stats;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
