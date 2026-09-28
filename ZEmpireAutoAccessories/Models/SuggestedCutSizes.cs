namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// A starting table of glass cut sizes, so a shop is not typing a hundred
    /// boxes from nothing.
    ///
    /// These are estimates, not measurements. They assume film about 60 inches
    /// wide and are scaled by class off one real figure - a sedan front
    /// windshield at 1.2 m. They are a floor to correct from, not an answer:
    /// measure one job, and if a sedan windshield really pulls 1.35 m, every
    /// column moves by the same ratio.
    ///
    /// Only glass is here. A full wrap, a siding or a painted door is a
    /// different order of magnitude - fifteen to twenty-five metres, not one
    /// or two - and guessing at those would put a badly wrong number on the
    /// shelf, so they are left blank on purpose.
    ///
    /// Matching is by name and case-insensitive, and a name nobody has is
    /// skipped. Panels that are two names for one thing - "Front Windshield"
    /// and "Front WS" - both resolve, so a shop carrying the pair gets the
    /// same size on each rather than one blank.
    /// </summary>
    public static class SuggestedCutSizes
    {
        /// <summary>The panels this table covers, and the names each goes by.</summary>
        private static readonly (string Key, string[] Names)[] Panels =
        {
            ("front windshield", new[] { "Front Windshield", "Front WS", "Windshield" }),
            ("rear windshield",  new[] { "Rear Windshield", "Rear WS" }),
            ("front windows",    new[] { "2FWS", "2 FWS", "Front Windows", "2 Front Windows" }),
            ("rear windows",     new[] { "2RWS", "2 RWS", "Rear Windows", "2 Rear Windows" }),
            ("quarter",          new[] { "Qtr Window", "Quarter Window", "Qtr Glass", "Quarter Glass" }),
            ("sunroof",          new[] { "Sunroof", "Moonroof" })
        };

        /// <summary>
        /// Centimetres per class, per panel. A null is a panel that class does
        /// not normally have - a pickup has no sunroof and very little rear
        /// glass - and is left for someone to fill in if they do.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, decimal?>> ByClass =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Small"]                 = Row(110, 115,  90,  80, 35,  85),
                ["Hatchback"]             = Row(110, 115,  90,  80, 35,  85),
                ["Subcompact/Crossover"]  = Row(115, 125,  95,  85, 40,  90),
                ["Sedan"]                 = Row(120, 130, 100,  90, 40,  90),
                ["MPV"]                   = Row(130, 145, 110, 100, 50,  95),
                ["Pickup"]                = Row(130,  80, 110,  55, 30, null),
                ["SUV"]                   = Row(140, 150, 115, 105, 50,  95),
                ["Big SUV"]               = Row(150, 160, 125, 115, 55, 100),
                ["Van"]                   = Row(155, 170, 120, 110, 60, 100)
            };

        private static Dictionary<string, decimal?> Row(
            decimal? frontWindshield, decimal? rearWindshield, decimal? frontWindows,
            decimal? rearWindows, decimal? quarter, decimal? sunroof) =>
            new()
            {
                ["front windshield"] = frontWindshield,
                ["rear windshield"] = rearWindshield,
                ["front windows"] = frontWindows,
                ["rear windows"] = rearWindows,
                ["quarter"] = quarter,
                ["sunroof"] = sunroof
            };

        /// <summary>
        /// Centimetres suggested for this pairing, or null where there is no
        /// suggestion - an unknown class, a panel off this list, or one the
        /// class does not have.
        /// </summary>
        public static decimal? Centimeters(string? classificationName, string? panelName)
        {
            if (classificationName == null || panelName == null)
                return null;

            if (!ByClass.TryGetValue(classificationName.Trim(), out var row))
                return null;

            var key = Panels.FirstOrDefault(p =>
                p.Names.Any(n => string.Equals(n, panelName.Trim(), StringComparison.OrdinalIgnoreCase))).Key;

            return key != null && row.TryGetValue(key, out var cm) ? cm : null;
        }
    }
}
