using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services;
using ZEmpireAutoAccessories.Services.Interfaces;

// QuestPDF Community license - free for orgs/individuals under its revenue
// threshold, which this shop system qualifies for.
QuestPDF.Settings.License = LicenseType.Community;

var phCulture = CultureInfo.GetCultureInfo("en-PH");
CultureInfo.DefaultThreadCurrentCulture = phCulture;
CultureInfo.DefaultThreadCurrentUICulture = phCulture;
var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Add Entity Framework Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity mapped to the asp.* tables
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = false;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "ZEmpire.Auth";
    options.Cookie.HttpOnly = true;
});

// Register application services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Which product categories are measured off a roll. Read once at startup from
// appsettings.json so a new film category can be added without a code change
// or a schema change; an absent section keeps the built-in list.
ZEmpireAutoAccessories.Models.UnitOfMeasure.ConfigureLengthCategories(
    builder.Configuration.GetSection("Inventory:LengthCategories").Get<string[]>());
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<ICashieringService, CashieringService>();

var app = builder.Build();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// One-time bootstrap: ensure roles exist and give SQL-seeded users a password
try
{
    await IdentitySeeder.SeedAsync(
        app.Services,
        app.Configuration,
        app.Services.GetRequiredService<ILogger<Program>>());
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogError(ex, "Identity seeding failed at startup.");
}

app.Run();
