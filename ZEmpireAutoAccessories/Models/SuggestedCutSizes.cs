namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The shop's own glass cut sizes, as measured: the width and height of
    /// the piece of film each panel takes on each class of vehicle.
    ///
    /// These replaced a table of estimates scaled off one sedan windshield.
    /// They are the owner's measurements, so a figure here is a fact about the
    /// glass and not a guess - but it is a fact about the GLASS, not about the
    /// roll. How much comes off the roll depends on how wide the roll is, and
    /// the shop buys 30 inch film, so a 152.4 cm windshield has to be made of
    /// two strips. Centimeters() does that arithmetic; see Strip().
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
            ("front windows",    new[] { "2FWS", "2 FWS", "Front Windows", "2 Front Windows", "1st Row" }),
            ("rear windows",     new[] { "2RWS", "2 RWS", "Rear Windows", "2 Rear Windows", "2nd Row" }),
            ("rear windshield",  new[] { "Rear Windshield", "Rear WS" }),
            ("sunroof",          new[] { "Sunroof", "Moonroof" }),
            ("quarter",          new[] { "Qtr Window", "Quarter Window", "Qtr Glass", "Quarter Glass" }),

            // Not a body wrap: in this shop's pricing, Full Wrap is only ever
            // priced against tint films - Black Series, Clear Series, Profilm
            // Nano Ceramic - so it means the glass on the car. It is not
            // measured on its own; it is the four main panels added up, so it
            // can never disagree with them. See FullWrapPanels for why the
            // sunroof and the quarter glass are not in that four.
            ("all glass",        new[] { "Full Wrap" }),

            // This one is a body wrap. Whole Vehicle is priced against the
            // ProFilm PPF products and against the coatings; only the PPF
            // takes film, and a coating is a liquid whose category is not
            // measured by length, so it never reaches a cut size at all.
            ("whole body",       new[] { "Whole Vehicle" })
        };

        /// <summary>One piece of film, as cut: the glass it covers.</summary>
        private readonly record struct Glass(decimal Width, decimal Height);

        /// <summary>
        /// The glass, per class, per panel. A null is a panel that class does
        /// not normally have - a sedan has no sunroof or quarter glass on this
        /// sheet - and is left for someone to fill in if theirs does.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, Glass?>> ByClass =
            new(StringComparer.OrdinalIgnoreCase)
            {
                //                                   WINDSHIELD      1ST ROW         2ND ROW         REAR WS          SUNROOF         QUARTER
                ["Small"]                 = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  null,                null),
                ["Sedan"]                 = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 66.04m), null,                null),
                ["Pickup"]                = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(152.4m, 30.48m),  null,                null),
                ["Subcompact/Crossover"]  = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  G(152.4m, 68.58m),   G(152.4m, 33.02m)),
                ["SUV"]                   = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  G(68.58m, 40.64m),   G(152.4m, 38.01m)),
                ["MPV"]                   = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  G(152.4m, 68.58m),   G(152.4m, 38.01m)),
                ["Big SUV"]               = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  G(68.58m, 40.64m),   G(152.4m, 38.01m)),
                ["Van"]                   = Row(G(177.8m, 152.4m), G(111.76m, 93.98m), G(238.76m, 101.6m), G(152.4m, 53.34m), null,              null),

                // Not on the shop's sheet. A hatchback's glass is a small
                // sedan's apart from the tailgate screen, so it is given the
                // small sedan's figures rather than left blank - a blank would
                // stop every hatchback tint job. Measure one and correct it.
                ["Hatchback"]             = Row(G(152.4m, 152.4m), G(93.98m, 101.6m), G(83.82m, 50.8m), G(119.38m, 50.8m),  null,                null)
            };

        /// <summary>
        /// A body wrap, in centimetres off the roll. Not on the shop's glass
        /// sheet, and still an estimate: it depends on how much of the car is
        /// covered and how the cutter nests the pieces. Kept as it was.
        /// </summary>
        private static readonly Dictionary<string, decimal> WholeBody =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Small"] = 1400m,
                ["Hatchback"] = 1500m,
                ["Subcompact/Crossover"] = 1600m,
                ["Sedan"] = 1800m,
                ["MPV"] = 2100m,
                ["Pickup"] = 2000m,
                ["SUV"] = 2200m,
                ["Big SUV"] = 2500m,
                ["Van"] = 2600m
            };

        /// <summary>
        /// What a full wrap covers: the windshield, both rows and the rear
        /// screen. Not the sunroof and not the quarter glass, for two reasons
        /// that agree. They are priced as panels of their own, so a job that
        /// has them carries its own line and adding them here would deduct
        /// them twice. And the shop's own full-wrap figures say so - 342.9 cm
        /// for a Big SUV and 543.56 for a Van are within 3% of these four
        /// added up on 60 inch film, and nowhere near the six.
        /// </summary>
        private static readonly string[] FullWrapPanels =
            { "front windshield", "front windows", "rear windows", "rear windshield" };

        private static Glass? G(decimal width, decimal height) => new Glass(width, height);

        private static Dictionary<string, Glass?> Row(
            Glass? windshield, Glass? frontWindows, Glass? rearWindows,
            Glass? rearWindshield, Glass? sunroof, Glass? quarter) =>
            new()
            {
                ["front windshield"] = windshield,
                ["front windows"] = frontWindows,
                ["rear windows"] = rearWindows,
                ["rear windshield"] = rearWindshield,
                ["sunroof"] = sunroof,
                ["quarter"] = quarter
            };

        /// <summary>
        /// How wide the film on the roll is, in centimetres. The shop buys 30
        /// inch, which is 76.2 cm exactly. Every figure this class produces
        /// moves when this does - a windshield that is two strips on a 30 inch
        /// roll is one on a 60 inch roll, for half the length - so it is a
        /// setting rather than a constant.
        /// </summary>
        public const decimal DefaultRollWidthCm = 76.2m;

        private static decimal RollWidthCm = DefaultRollWidthCm;

        /// <summary>
        /// Points the roll width at configuration. An absent, zero or negative
        /// setting keeps 30 inch, so a missing section can never leave the app
        /// dividing by nothing.
        /// </summary>
        public static void ConfigureRollWidth(decimal? centimeters)
        {
            if (centimeters is > 0m)
                RollWidthCm = centimeters.Value;
        }

        /// <summary>The roll width currently being worked from.</summary>
        public static decimal ConfiguredRollWidthCm => RollWidthCm;

        /// <summary>
        /// How much length a piece of glass takes off the roll.
        ///
        /// A piece wider than the roll cannot be cut in one, so it is made of
        /// strips laid side by side: three strips of a 152 cm windshield off a
        /// 76 cm roll is three times the windshield's height. The piece can be
        /// turned, and which way round costs less is not always obvious - a
        /// 119 x 51 rear screen is one strip of 119 but two strips of 51 - so
        /// both are worked out and the cheaper taken, which is what a cutter
        /// does by eye.
        /// </summary>
        private static decimal Strip(Glass glass, decimal rollWidth)
        {
            var acrossTheWidth = Math.Ceiling(glass.Width / rollWidth) * glass.Height;
            var acrossTheHeight = Math.Ceiling(glass.Height / rollWidth) * glass.Width;

            return decimal.Round(Math.Min(acrossTheWidth, acrossTheHeight), 2,
                                 MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Centimetres off the roll for this pairing, or null where there is
        /// no figure - an unknown class, a panel off this list, or one the
        /// class does not have.
        /// </summary>
        public static decimal? Centimeters(string? classificationName, string? panelName)
        {
            if (classificationName == null || panelName == null)
                return null;

            var className = classificationName.Trim();

            var key = Panels.FirstOrDefault(p =>
                p.Names.Any(n => string.Equals(n, panelName.Trim(), StringComparison.OrdinalIgnoreCase))).Key;

            if (key == null)
                return null;

            if (key == "whole body")
                return WholeBody.TryGetValue(className, out var body) ? body : null;

            if (!ByClass.TryGetValue(className, out var row))
                return null;

            // Every pane added up. Taken from the same measurements rather
            // than carried as its own number, so a full job and the panels
            // that make it up can never drift apart.
            if (key == "all glass")
            {
                var panes = FullWrapPanels
                    .Select(p => row.TryGetValue(p, out var g) ? g : null)
                    .Where(g => g != null)
                    .Select(g => Strip(g!.Value, RollWidthCm))
                    .ToList();

                return panes.Count == 0 ? null : panes.Sum();
            }

            return row.TryGetValue(key, out var glass) && glass != null
                ? Strip(glass.Value, RollWidthCm)
                : null;
        }
    }
}
