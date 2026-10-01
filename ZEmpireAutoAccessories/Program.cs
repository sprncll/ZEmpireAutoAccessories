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

// Categories that are two names for the same shelf. The screens group and
// label by the canonical name so one shelf shows as one heading.
ZEmpireAutoAccessories.Models.ProductCategories.ConfigureAliases(
    builder.Configuration.GetSection("Catalog:EquivalentCategories").Get<Dictionary<string, string>>());
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<ICashieringService, CashieringService>();

// Where files kept as evidence live - proof an online payment arrived, proof
// a warranty was claimed - and which payment modes need one.
builder.Services.AddScoped<IProofStore, ProofStore>();

// How much film each panel takes on each class of vehicle, and what that
// makes a document's completion deduct. The cut sizes are a singleton
// because the point of them is the cache; see CutSizeStore.
builder.Services.AddSingleton<ICutSizeStore, CutSizeStore>();
builder.Services.AddScoped<IDocumentStockService, DocumentStockService>();

// Products and categories someone has set by hand as roll goods or pieces,
// overriding the category-name list above. A singleton for its cache, and
// built eagerly below so the overrides are in place before the first request
// rather than when a screen first happens to inject it.
builder.Services.AddSingleton<IMeasureStore, MeasureStore>();

// What each completed document took off the shelf, so reversing it takes back
// the same even after a cut size is corrected. A singleton for its cache, the
// same as the cut sizes.
builder.Services.AddSingleton<IStockPostingLog, StockPostingLog>();
ZEmpireAutoAccessories.Models.PaymentModes.Configure(
    builder.Configuration.GetSection("Payments:Modes").Get<string[]>(),
    builder.Configuration.GetSection("Payments:ModesNeedingProof").Get<string[]>(),
    builder.Configuration.GetSection("Payments:RenamedModes").Get<Dictionary<string, string>>(),
    builder.Configuration.GetSection("Payments:ModesGivingChange").Get<string[]>());

var app = builder.Build();

// Constructing it loads the file and hands the overrides to UnitOfMeasure,
// which every screen reads. Do it now: until it happens, a product set by
// hand reads as whatever its category name says, which for a measured product
// means one centimetre coming off the roll instead of a whole windshield.
app.Services.GetRequiredService<IMeasureStore>();

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

    // The payment modes the shop offers, inserted if the table does not
    // already carry them. Insert-only - see PaymentModeSeeder.
    await PaymentModeSeeder.SeedAsync(
        app.Services,
        app.Services.GetRequiredService<ILogger<Program>>());

    // The shades each tint variant comes in. Also insert-only.
    await ShadeSeeder.SeedAsync(
        app.Services,
        app.Services.GetRequiredService<ILogger<Program>>());
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogError(ex, "Identity seeding failed at startup.");
}

app.Run();
