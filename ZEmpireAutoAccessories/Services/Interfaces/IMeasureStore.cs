namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// Whether a product is measured off a roll or counted in pieces, where
    /// someone has said so by hand. See MeasureStore for why this is a file
    /// rather than a column.
    /// </summary>
    public interface IMeasureStore
    {
        /// <summary>
        /// This product's own setting, or null when it has none and the
        /// category decides. Null is not "pieces" - it is "nobody has said",
        /// which the forms show differently.
        /// </summary>
        bool? ForProduct(int productId);

        /// <summary>This category's own setting, or null when its name decides.</summary>
        bool? ForCategory(int categoryId);

        /// <summary>
        /// Sets or clears one product. Null clears it, putting the product
        /// back under whatever its category says.
        /// </summary>
        Task SetProduct(int productId, bool? soldByLength);

        /// <summary>Sets or clears one category.</summary>
        Task SetCategory(int categoryId, bool? soldByLength);

        /// <summary>Forgets a product that has been deleted.</summary>
        Task ForgetProduct(int productId);

        /// <summary>Forgets a category that has been deleted.</summary>
        Task ForgetCategory(int categoryId);
    }
}
