namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Paint Protection Film and Window Tint come off a roll and are measured
    /// by length, not counted in pieces. People work in whatever unit suits
    /// the job - a supplier delivers 30 m, a staff member cuts 18 in - so the
    /// unit is chosen at the point of entry and converted here.
    ///
    /// Everything is stored in CENTIMETRES. inv.InventoryTransaction has no
    /// unit column, so a single base unit is the only way a running total
    /// means anything; centimetres keep every conversion exact in decimal
    /// (1 in = 2.54 cm, 1 m = 100 cm) with no repeating fractions.
    ///
    /// Products in any other category stay as they were: whole pieces, no
    /// conversion, no unit picker.
    /// </summary>
    public static class UnitOfMeasure
    {
        public const string Centimeter = "cm";
        public const string Inch = "in";
        public const string Meter = "m";
        public const string Piece = "Piece";

        /// <summary>The unit everything is stored and totalled in.</summary>
        public const string Base = Centimeter;

        /// <summary>
        /// Categories bought and sold by length. Matching on the category name
        /// keeps this out of the database, which has no column to mark a
        /// product as roll goods.
        /// </summary>
        private static readonly HashSet<string> LengthCategories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Paint Protection Film",
                "Window Tint"
            };

        /// <summary>How many centimetres one of each unit is. Exact in decimal.</summary>
        private static readonly Dictionary<string, decimal> Centimeters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [Centimeter] = 1m,
                [Inch] = 2.54m,
                [Meter] = 100m
            };

        /// <summary>The units offered for roll goods, in the order they are shown.</summary>
        public static readonly string[] LengthUnits = { Centimeter, Inch, Meter };

        public static bool IsSoldByLength(string? categoryName) =>
            categoryName != null && LengthCategories.Contains(categoryName);

        public static bool IsLengthUnit(string? unit) =>
            unit != null && Centimeters.ContainsKey(unit);

        /// <summary>
        /// A quantity typed in <paramref name="unit"/>, as centimetres. An
        /// unrecognised or absent unit is already a base quantity and passes
        /// through untouched, so piece goods and internal callers that work in
        /// centimetres are unaffected.
        /// </summary>
        public static decimal ToBase(decimal quantity, string? unit) =>
            unit != null && Centimeters.TryGetValue(unit, out var perUnit)
                ? quantity * perUnit
                : quantity;

        /// <summary>Centimetres expressed in <paramref name="unit"/>.</summary>
        public static decimal FromBase(decimal centimeters, string? unit) =>
            unit != null && Centimeters.TryGetValue(unit, out var perUnit)
                ? centimeters / perUnit
                : centimeters;

        /// <summary>
        /// A stock figure for reading: metres once there is a metre of it,
        /// centimetres below that, and a plain count for piece goods. Rolls
        /// are ordered in metres, so "12.5 m" is the figure someone can act
        /// on where "1250" is not.
        /// </summary>
        public static string Describe(decimal quantity, bool soldByLength)
        {
            if (!soldByLength)
                return quantity.ToString("N0");

            return quantity >= 100m
                ? $"{FromBase(quantity, Meter):N2} m"
                : $"{quantity:N1} cm";
        }

        /// <summary>The unit label to store on a document line.</summary>
        public static string LineUnit(bool soldByLength, string? chosenUnit) =>
            soldByLength && IsLengthUnit(chosenUnit) ? chosenUnit! : "Unit";
    }
}
