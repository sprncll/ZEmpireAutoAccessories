using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// How much of each shade is left, derived from the last stock count and
    /// the document lines since. See ShadeStock for why this is worked out
    /// rather than read.
    /// </summary>
    public interface IShadeStockService
    {
        /// <summary>
        /// Every roll product that has shades on file, with each shade's
        /// standing. Products whose variants carry no shades are left out -
        /// there is nothing to break down.
        /// </summary>
        /// <param name="productId">
        /// One product's shades rather than the whole catalogue's. The product
        /// set seeded here scopes every query behind it, so this narrows the
        /// work and not just the result.
        /// </param>
        Task<List<ShadeStockProduct>> Build(int? productId = null);
    }
}
