using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// What a line costs - the only place that says so.
    ///
    /// Prices are kept in the price records and nowhere else: a film's price
    /// per panel and class of vehicle in Pricing, a product's or a service's
    /// own price on its record. Every form that writes a line - job order,
    /// quotation, service invoice, the counter sale - takes its price from
    /// here, for everybody. Nobody types one, Admin included: an Admin
    /// changes what the record says, under Pricing, Products or Services,
    /// and that change is logged once, rather than a different figure being
    /// typed onto one document where nobody will see it.
    ///
    /// A line the records have no price for is refused, with the record to
    /// set, rather than going out at nothing or at a figure somebody made up.
    /// </summary>
    public class PriceBook
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventory;

        public PriceBook(ApplicationDbContext context, IInventoryService inventory)
        {
            _context = context;
            _inventory = inventory;
        }

        public sealed record Price(decimal? Amount, int? PricingId, string? Missing)
        {
            public bool Found => Amount is > 0m;
        }

        /// <summary>
        /// The price of one line. For tint and film that is the Pricing row
        /// for this product, variant, class of vehicle and panel - never the
        /// product's flat price, which is not the price of any one panel.
        /// </summary>
        public async Task<Price> For(int? productId, int? serviceId, int? tintVariantId,
                                     int? vehicleClassificationId, int? panelId)
        {
            if (serviceId != null)
            {
                var service = await _context.Services
                    .Where(s => s.ServiceID == serviceId)
                    .Select(s => new { s.ServiceName, s.DefaultPrice })
                    .FirstOrDefaultAsync();

                return service?.DefaultPrice is > 0m
                    ? new Price(service.DefaultPrice, null, null)
                    : new Price(null, null,
                        $"\"{service?.ServiceName}\" has no price yet. An administrator sets it under Operations → Services.");
            }

            if (productId == null)
                return new Price(null, null, "Pick a product or a service.");

            var product = await _context.Products
                .Where(p => p.ProductID == productId)
                .Select(p => new { p.ProductName, p.DefaultPrice })
                .FirstOrDefaultAsync();

            var film = (await _inventory.SoldByLength(new[] { productId.Value })).Count > 0;

            if (film)
            {
                var row = panelId == null || vehicleClassificationId == null
                    ? null
                    : await _context.Pricings
                        .Where(p => p.ProductID == productId
                                 && p.TintVariantID == tintVariantId
                                 && p.VehicleClassificationID == vehicleClassificationId
                                 && p.PanelID == panelId)
                        .Select(p => new { p.PricingID, p.Price })
                        .FirstOrDefaultAsync();

                if (row is { Price: > 0m })
                    return new Price(row.Price, row.PricingID, null);

                var className = vehicleClassificationId == null ? null
                    : await _context.VehicleClassifications
                        .Where(c => c.VehicleClassificationID == vehicleClassificationId)
                        .Select(c => c.ClassificationName).FirstOrDefaultAsync();
                var panelName = panelId == null ? null
                    : await _context.Panels.Where(p => p.PanelID == panelId).Select(p => p.PanelName).FirstOrDefaultAsync();
                var variantName = tintVariantId == null ? null
                    : await _context.TintVariants.Where(v => v.TintVariantID == tintVariantId).Select(v => v.VariantName).FirstOrDefaultAsync();

                if (vehicleClassificationId == null)
                    return new Price(null, null,
                        "This vehicle has no vehicle class, so there is no price for its panels. Set its class on the vehicle's record.");

                var what = string.Join(" · ", new[] { product?.ProductName, variantName, panelName }.Where(x => !string.IsNullOrEmpty(x)));
                return new Price(null, null,
                    $"There is no price for {what} on a {className} yet. An administrator sets it under Catalog & Inventory → Pricing.");
            }

            return product?.DefaultPrice is > 0m
                ? new Price(product.DefaultPrice, null, null)
                : new Price(null, null,
                    $"\"{product?.ProductName}\" has no price yet. An administrator sets it under Catalog & Inventory → Products.");
        }
    }
}
