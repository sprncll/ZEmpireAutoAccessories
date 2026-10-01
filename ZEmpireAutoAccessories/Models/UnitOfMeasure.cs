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
        /// The categories bought and sold by length if nothing overrides them.
        /// Matching on the category name keeps this out of the database, which
        /// has no column to mark a product as roll goods.
        /// </summary>
        private static readonly string[] DefaultLengthCategories =
        {
            "Paint Protection Film",
            "Paint Protection",
            "Window Tint",
            "Tint"
        };

        /// <summary>
        /// Replaced once at startup from appsettings.json when that file lists
        /// categories, so a new film category can be added without a code
        /// change or a schema change. Assigned whole rather than mutated, and
        /// only during startup, so readers always see a complete set.
        /// </summary>
        private static HashSet<string> LengthCategories =
            new(DefaultLengthCategories, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Points the category list at configuration. An absent or empty
        /// setting keeps the built-in list, so a missing section can never
        /// leave the app with nothing measured by length.
        /// </summary>
        public static void ConfigureLengthCategories(IEnumerable<string>? categoryNames)
        {
            var names = categoryNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .ToList();

            if (names is { Count: > 0 })
                LengthCategories = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The category names currently treated as roll goods.</summary>
        public static IReadOnlyCollection<string> ConfiguredLengthCategories => LengthCategories;

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

        /// <summary>
        /// The units offered for anything counted rather than measured.
        ///
        /// "Unit" used to head this list and was the default, but it is not a
        /// unit of measure - it says nothing about what one line covers. And
        /// "roll" sat here as a count while a roll of film is measured in
        /// cm/in/m, so the same word meant two different things depending on
        /// the screen. Both are gone; a piece is the sensible default.
        /// </summary>
        public static readonly string[] CountUnits = { "pc", "set", "pair", "job" };

        /// <summary>What a line is written in when nothing else is chosen.</summary>
        public const string DefaultCountUnit = "pc";

        /// <summary>
        /// Products and categories that have been set by hand, overriding the
        /// category-name list. cat.Product has no column for this and nothing
        /// may be added to the schema, so the choices live in a JSON file that
        /// MeasureStore loads at startup and pushes in here - the same shape
        /// as the category list above, replaced whole rather than mutated so
        /// a reader never sees half an update.
        ///
        /// A product beats its category, and a category beats the name list.
        /// Absent from both means the name list decides, which is what every
        /// product did before any of this existed.
        /// </summary>
        private static IReadOnlyDictionary<int, bool> ProductOverrides =
            new Dictionary<int, bool>();

        private static IReadOnlyDictionary<int, bool> CategoryOverrides =
            new Dictionary<int, bool>();

        /// <summary>
        /// Replaces the hand-set overrides. Called once at startup and again
        /// whenever someone saves a product or a category.
        /// </summary>
        public static void ConfigureOverrides(
            IReadOnlyDictionary<int, bool>? products,
            IReadOnlyDictionary<int, bool>? categories)
        {
            ProductOverrides = products ?? new Dictionary<int, bool>();
            CategoryOverrides = categories ?? new Dictionary<int, bool>();
        }

        /// <summary>
        /// Whether a category's products come off a roll, by its own setting
        /// if it has one and otherwise by its name.
        /// </summary>
        public static bool ForCategory(int categoryId, string? categoryName) =>
            CategoryOverrides.TryGetValue(categoryId, out var set)
                ? set
                : IsSoldByLength(categoryName);

        /// <summary>
        /// Whether this product comes off a roll. Its own setting wins, so a
        /// boxed pre-cut kit can sit in a film category and still be counted
        /// in pieces, and a roll of something filed elsewhere can be measured.
        /// </summary>
        public static bool ForProduct(int productId, int categoryId, string? categoryName) =>
            ProductOverrides.TryGetValue(productId, out var set)
                ? set
                : ForCategory(categoryId, categoryName);

        /// <summary>Whether a category NAME alone reads as roll goods.</summary>
        public static bool IsSoldByLength(string? categoryName) =>
            categoryName != null && LengthCategories.Contains(categoryName.Trim());

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

        /// <summary>
        /// A stock figure split into the number and its unit, so a column can
        /// align the numbers and set the units apart. Piece goods get a unit
        /// too ("pcs") - a column mixing "4.32 m" with a bare "100" reads as
        /// though one of them is missing something.
        /// </summary>
        public static (string Value, string Unit) Split(decimal quantity, bool soldByLength)
        {
            if (!soldByLength)
                return (quantity.ToString("N0"), "pcs");

            return quantity >= 100m
                ? (FromBase(quantity, Meter).ToString("N2"), Meter)
                // Trailing zeros on a centimetre reading are noise: 99 cm, not 99.0 cm.
                : (quantity.ToString("0.##"), Centimeter);
        }

        /// <summary>The unit label to store on a document line.</summary>
        public static string LineUnit(bool soldByLength, string? chosenUnit) =>
            soldByLength && IsLengthUnit(chosenUnit) ? chosenUnit! : (chosenUnit ?? DefaultCountUnit);
    }
}
