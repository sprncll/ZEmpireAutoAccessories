namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The catalogue has grown more than one name for the same shelf:
    /// "Paint Protection" and "Paint Protection Film" are the same thing, as
    /// are "Tint" and "Window Tint". Products sit under whichever was picked
    /// at the time, so the screens showed two bands for one shelf.
    ///
    /// Rather than rewrite the data, the screens resolve a category to its
    /// canonical name before grouping or labelling. One shelf, one heading,
    /// wherever a product's category is shown.
    ///
    /// The Product form goes further and offers only canonical categories,
    /// so nothing new lands under a duplicate name - except for a product
    /// already filed under one, which keeps its own category on the list so
    /// editing it does not silently move it somewhere else.
    /// </summary>
    public static class ProductCategories
    {
        /// <summary>
        /// alias -> the name to show instead. Case-insensitive, and applied
        /// once (an alias must point at a real category, not another alias).
        /// </summary>
        private static readonly Dictionary<string, string> DefaultAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Paint Protection"] = "Paint Protection Film",
                ["Tint"] = "Window Tint"
            };

        private static Dictionary<string, string> Aliases =
            new(DefaultAliases, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Points the alias list at configuration, so another pair of names
        /// can be folded together without a code change. An absent or empty
        /// setting keeps the built-in pairs.
        /// </summary>
        public static void ConfigureAliases(IDictionary<string, string>? aliases)
        {
            var cleaned = aliases?
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key.Trim(), kv => kv.Value.Trim(), StringComparer.OrdinalIgnoreCase);

            if (cleaned is { Count: > 0 })
                Aliases = cleaned;
        }

        /// <summary>
        /// The name this category should be shown and grouped under. A
        /// category that is nobody's alias is already canonical and comes
        /// back unchanged.
        /// </summary>
        public static string Canonical(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "Uncategorised";

            var name = categoryName.Trim();
            return Aliases.TryGetValue(name, out var canonical) ? canonical : name;
        }

        /// <summary>
        /// Whether this category is a duplicate of another - true when it has
        /// a canonical name that is not itself.
        /// </summary>
        public static bool IsDuplicate(string? categoryName) =>
            !string.IsNullOrWhiteSpace(categoryName)
            && !string.Equals(Canonical(categoryName), categoryName.Trim(),
                              StringComparison.OrdinalIgnoreCase);
    }
}
