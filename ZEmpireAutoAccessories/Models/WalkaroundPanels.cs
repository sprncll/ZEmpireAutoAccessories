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
        /// Front to back, the way someone actually walks around a car:
        /// windshield, front doors, rear doors, quarter glass, rear screen,
        /// then the roof. Alphabetical order put "2FWS" first and the
        /// windshield third, which matches nothing anyone does.
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
            new[] { "Sunroof", "Moonroof" }
        };

        /// <summary>A price for several panels at once, not a piece of glass.</summary>
        private static readonly string[] Bundles = { "Full Wrap", "Whole Vehicle" };

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
