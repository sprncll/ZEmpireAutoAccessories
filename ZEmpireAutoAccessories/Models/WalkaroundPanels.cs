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
        private static readonly string[][] Order =
        {
            new[] { "Front Windshield", "Front WS", "Windshield" },
            new[] { "2FWS", "2 FWS", "Front Windows", "2 Front Windows", "1st Row" },
            new[] { "2RWS", "2 RWS", "Rear Windows", "2 Rear Windows", "2nd Row" },
            new[] { "Qtr Window", "Quarter Window", "Qtr Glass", "Quarter Glass" },
            new[] { "Rear Windshield", "Rear WS" },
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
        /// The sheet to walk: every panel that is a piece of glass, plus any
        /// bundle that already carries a recorded condition, in walkaround
        /// order.
        /// </summary>
        public static List<Panel> Walkaround(IEnumerable<Panel> all, IEnumerable<int> alreadyRecorded)
        {
            var recorded = alreadyRecorded.ToHashSet();

            return all
                .Where(p => !IsBundle(p.PanelName) || recorded.Contains(p.PanelID))
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
