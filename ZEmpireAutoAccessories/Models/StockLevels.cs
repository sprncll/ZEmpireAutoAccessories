namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// When a product counts as low, and when it counts as out.
    ///
    /// This was the number 5, written out in three places - the inventory
    /// controller, the stock count controller and the inventory view - which
    /// could drift from one another, and did say different things to the
    /// dashboard and the list.
    ///
    /// It was also one number for two units. Stock is held in pieces for
    /// counted goods and CENTIMETRES for film, so "5" meant five horns or
    /// five centimetres of tint - two inches, a roll already finished. A film
    /// did not show as low until it was gone. The two are separate now, and
    /// both come from the settings screen.
    /// </summary>
    public static class StockLevels
    {
        /// <summary>Pieces, for anything counted.</summary>
        public static decimal Pieces { get; private set; } = 5m;

        /// <summary>Centimetres, for anything off a roll. Five metres by default.</summary>
        public static decimal Centimetres { get; private set; } = 500m;

        /// <summary>
        /// Replaced whole when the settings are saved, and once at startup.
        /// A figure at or below zero would make everything low, or nothing,
        /// so it is ignored in favour of what is already set.
        /// </summary>
        public static void Configure(decimal pieces, decimal metres)
        {
            if (pieces > 0) { Pieces = pieces; }
            if (metres > 0) { Centimetres = metres * 100m; }
        }

        /// <summary>The point this product is measured against, in its own unit.</summary>
        public static decimal Threshold(bool soldByLength) => soldByLength ? Centimetres : Pieces;

        /// <summary>Nothing left at all.</summary>
        public static bool IsOut(decimal stock) => stock <= 0m;

        /// <summary>Some left, but not much - which is a different thing from none.</summary>
        public static bool IsLow(decimal stock, bool soldByLength) =>
            stock > 0m && stock <= Threshold(soldByLength);

        /// <summary>Comfortably stocked.</summary>
        public static bool IsFine(decimal stock, bool soldByLength) =>
            stock > Threshold(soldByLength);
    }
}
