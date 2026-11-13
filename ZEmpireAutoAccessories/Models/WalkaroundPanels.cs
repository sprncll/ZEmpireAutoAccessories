namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// Which panels a condition walkaround asks about, and in what order.
    ///
    /// The checklist points at <c>cat.Panel</c>, which is the PRICING
    /// catalogue: alongside the glass it carries "Full Wrap" and "Whole
    /// Vehicle", which are bundles - one price covering several panels at
    /// once. Nobody can inspect a bundle, and offering them on the walkaround
    /// meant the windshield could be recorded twice, once on its own and once
    /// inside Full Wrap. They are left off the sheet.
    ///
    /// Off the sheet, not forbidden: a bundle that already has a condition
    /// recorded against it is still shown, because hiding a row is worse than
    /// offering one nobody wants. See <see cref="Walkaround"/>.
    ///
    /// The names repeat <see cref="SuggestedCutSizes"/>' aliases rather than
    /// borrowing them - that table deliberately includes the bundles, since
    /// pricing needs them, and this one deliberately does not.
    /// </summary>
    public static class WalkaroundPanels
    {
        /// <summary>
        /// The order somebody actually walks a car: the glass front to back,
        /// then the bodywork, then inside. Alphabetical order put "2FWS"
        /// first and the windshield third, which matches nothing anyone does,
        /// and left the dashboard between the doors.
        ///
        /// A panel the shop adds that is not listed here still appears - it
        /// sorts after everything that is, by name - so nothing is lost by
        /// this list being incomplete.
        /// </summary>
        /// <summary>
        /// The name FIRST in each group is the one the price scripts create,
        /// and so the one the pricing matrix, the job order panel dropdown and
        /// the cut-size table are all keyed on. Where two rows in cat.Panel
        /// name the same glass, that is the one the walkaround keeps, so a
        /// condition is recorded against the same panel the work is priced
        /// against. The rest are names the same glass has been given
        /// elsewhere.
        /// </summary>
        private static readonly string[][] Order =
        {
            new[] { "Front WS", "Front Windshield", "Windshield" },
            new[] { "2FWS", "2 FWS", "Front Windows", "2 Front Windows", "1st Row" },
            new[] { "2RWS", "2 RWS", "Rear Windows", "2 Rear Windows", "2nd Row" },
            new[] { "Qtr Window", "Quarter Window", "Qtr Glass", "Quarter Glass" },
            new[] { "Rear WS", "Rear Windshield" },
            new[] { "Sunroof", "Moonroof" },

            // Then the bodywork, front to back, the same walk continued.
            new[] { "Front Doors" },
            new[] { "Rear Doors" },
            new[] { "Sidings" },

            // Then inside, which is a second pass: the car is handed over
            // whole, and a mark on a seat afterwards is an argument the shop
            // has no way to answer unless somebody looked first.
            new[] { "Dashboard" },
            new[] { "Seats", "Front Seats", "Rear Seats" },
            new[] { "Headliner", "Roof Lining" },
            new[] { "Door Panels", "Door Cards" },
            new[] { "Carpet", "Flooring", "Floor Mats" }
        };

        /// <summary>A price for several panels at once, not a piece of glass.</summary>
        private static readonly string[] Bundles = { "Full Wrap", "Whole Vehicle" };

        // The inside of the car. Walked on the condition sheet - a mark on a
        // seat is worth recording on arrival - but never something tint or
        // film is cut for.
        private static readonly string[] InteriorNames =
        {
            "Dashboard", "Seats", "Front Seats", "Rear Seats", "Headliner", "Roof Lining",
            "Door Panels", "Door Cards", "Carpet", "Flooring", "Floor Mats",
        };

        public static bool IsInterior(string? panelName) =>
            panelName != null && InteriorNames.Contains(panelName.Trim(), StringComparer.OrdinalIgnoreCase);

        public static bool IsBundle(string? panelName) =>
            panelName != null && Bundles.Contains(panelName.Trim(), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Where a panel sits on the walkaround. A panel this shop added that
        /// is not in the list above sorts after the ones that are, by name,
        /// rather than being dropped.
        /// </summary>
        public static int Rank(string? panelName)
        {
            if (string.IsNullOrWhiteSpace(panelName))
                return int.MaxValue;

            var name = panelName.Trim();

            for (var i = 0; i < Order.Length; i++)
            {
                if (Order[i].Contains(name, StringComparer.OrdinalIgnoreCase))
                    return i;
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Which alias group a panel belongs to, for collapsing two rows that
        /// name the same piece of glass. A panel this list has never heard of
        /// is its own group, so two genuinely different panels are never
        /// merged on a guess.
        /// </summary>
        private static string GroupKey(string? panelName)
        {
            var rank = Rank(panelName);

            return rank == int.MaxValue
                ? "name:" + (panelName?.Trim().ToLowerInvariant() ?? string.Empty)
                : "alias:" + rank;
        }

        /// <summary>
        /// Exactly one row per piece of glass, out of rows that name the same
        /// one.
        ///
        /// cat.Panel is the pricing catalogue and nothing stops two rows
        /// meaning the same thing: "Front WS" is what the price scripts
        /// create, and a "Front Windshield" added later by hand sits beside
        /// it. On a price list that is untidy; on a walkaround it is one
        /// windscreen asked about twice, and two answers that can disagree -
        /// which is exactly what a checklist exists to prevent.
        ///
        /// Which row wins, in order:
        ///
        ///   1. the canonical one, if it carries a condition. That is the
        ///      name the price scripts create, so the pricing matrix, the job
        ///      order panel dropdown and the cut-size table are all keyed on
        ///      it, and a condition recorded against it points at the same
        ///      row the work is priced against;
        ///   2. whichever row does carry one, if the canonical does not - an
        ///      observation on screen beats an empty box;
        ///   3. the canonical row, when neither has been answered.
        ///
        /// Nothing is deleted. A condition on the row that loses stays in
        /// ops.VehicleChecklistDetail untouched - SaveDetails only writes
        /// panels the form posted, so a hidden row is not cleared by saving
        /// the sheet. Database/FindDuplicatePanels.sql merges them properly.
        /// </summary>
        private static IEnumerable<Panel> OnePerPanel(
            IEnumerable<Panel> group, IReadOnlySet<int> recorded)
        {
            var rows = group.ToList();

            if (rows.Count == 1)
                return rows;

            var canonical = rows.FirstOrDefault(p =>
                Order.Any(names => names.Length > 0
                    && string.Equals(names[0], p.PanelName?.Trim(), StringComparison.OrdinalIgnoreCase)));

            if (canonical != null && recorded.Contains(canonical.PanelID))
                return new[] { canonical };

            var answered = rows
                .Where(p => recorded.Contains(p.PanelID))
                .OrderBy(p => p.PanelID)
                .FirstOrDefault();

            return new[] { answered ?? canonical ?? rows.OrderBy(p => p.PanelID).First() };
        }

        private static readonly HashSet<int> NothingRecorded = new();

        /// <summary>
        /// The panels a LINE ITEM can be priced against: one row per panel, in
        /// the same walkaround order, with the bundles kept and sent to the
        /// end.
        ///
        /// Two differences from <see cref="Walkaround"/>, and both matter.
        /// "Full Wrap" and "Whole Vehicle" are nonsense on a condition sheet
        /// but perfectly ordinary on a price list, so they stay. And nothing
        /// has been recorded against these - there is no checklist here - so
        /// where two rows name the same glass the canonical one wins, which is
        /// the name the price scripts key on.
        ///
        /// The inside of the car is left out: this is the list a tint or film
        /// line picks its panel from, and the dashboard, seats, headliner,
        /// door panels and carpet are never cut for. They stay on the
        /// condition sheet, which is where they belong.
        /// </summary>
        public static List<Panel> ForPricing(IEnumerable<Panel> all) =>
            all
                .Where(p => !IsInterior(p.PanelName))
                .GroupBy(p => GroupKey(p.PanelName))
                .SelectMany(group => OnePerPanel(group, NothingRecorded))
                .OrderBy(p => IsBundle(p.PanelName) ? 1 : 0)
                .ThenBy(p => Rank(p.PanelName))
                .ThenBy(p => p.PanelName, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// The sheet to walk: one row per piece of glass, plus any bundle that
        /// already carries a recorded condition, in walkaround order.
        /// </summary>
        public static List<Panel> Walkaround(IEnumerable<Panel> all, IEnumerable<int> alreadyRecorded)
        {
            var recorded = alreadyRecorded.ToHashSet();

            return all
                .Where(p => !IsBundle(p.PanelName) || recorded.Contains(p.PanelID))
                .GroupBy(p => GroupKey(p.PanelName))
                .SelectMany(group => OnePerPanel(group, recorded))
                .OrderBy(p => Rank(p.PanelName))
                .ThenBy(p => p.PanelName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The sheet as the form renders it: one row per panel, carrying
        /// whatever is already recorded, or whatever was just typed if a post
        /// came back with an error.
        /// </summary>
        public static List<WalkaroundRow> Rows(
            IEnumerable<Panel> walkaround,
            IEnumerable<VehicleChecklistDetail> details,
            IReadOnlyDictionary<int, (string? Condition, string? Notes)>? typed = null)
        {
            var recorded = details.ToDictionary(d => d.PanelID);

            return walkaround.Select(panel =>
            {
                string? condition = null;
                string? notes = null;

                if (typed != null && typed.TryGetValue(panel.PanelID, out var just))
                {
                    condition = just.Condition;
                    notes = just.Notes;
                }
                else if (recorded.TryGetValue(panel.PanelID, out var detail))
                {
                    condition = detail.ExistingCondition;
                    notes = detail.Notes;
                }

                return new WalkaroundRow
                {
                    PanelID = panel.PanelID,
                    PanelName = panel.PanelName,
                    Condition = condition,
                    Notes = notes,
                    IsBundle = IsBundle(panel.PanelName)
                };
            }).ToList();
        }
    }

    /// <summary>One line of the walkaround form.</summary>
    public class WalkaroundRow
    {
        public int PanelID { get; init; }
        public string PanelName { get; init; } = string.Empty;
        public string? Condition { get; init; }
        public string? Notes { get; init; }

        /// <summary>
        /// A pricing bundle rather than a piece of glass, shown only because
        /// it already carries a condition from before the sheet was narrowed
        /// to the glass.
        /// </summary>
        public bool IsBundle { get; init; }

        public ConditionTone Tone => PanelConditions.ToneOf(Condition, Notes);

        /// <summary>
        /// Whether the stored condition is one of the choices. Free text
        /// typed before the choices existed is kept and offered back as its
        /// own option rather than being quietly replaced.
        /// </summary>
        public bool HasFreeText =>
            !string.IsNullOrWhiteSpace(Condition) && !PanelConditions.IsKnown(Condition);
    }
}
