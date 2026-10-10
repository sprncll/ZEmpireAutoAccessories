namespace ZEmpireAutoAccessories.Models
{
    /// <summary>What a panel's recorded condition amounts to.</summary>
    public enum ConditionTone
    {
        /// <summary>Nobody has looked at this panel yet.</summary>
        NotRecorded,

        /// <summary>Looked at, nothing wrong with it.</summary>
        Clear,

        /// <summary>This car does not have this panel - no sunroof, no quarter glass.</summary>
        NotApplicable,

        /// <summary>Something was written down that is not one of the choices.</summary>
        Noted,

        /// <summary>The panel came in damaged.</summary>
        Issue
    }

    /// <summary>
    /// The conditions staff pick from on a walkaround.
    ///
    /// They are stored as text in <c>ops.VehicleChecklistDetail.ExistingCondition</c>,
    /// which is where free text was stored before - so every condition typed
    /// by hand up to now still reads back exactly as it was typed. A value
    /// that is not one of these choices is reported as
    /// <see cref="ConditionTone.Noted"/> rather than being corrected or
    /// counted as damage: the shop wrote it, the shop can mean it.
    /// </summary>
    public static class PanelConditions
    {
        public const string Good = "Good";
        public const string NotApplicable = "Not applicable";

        /// <summary>
        /// The choices, in the order they are offered. "Old film / residue"
        /// is here because it is the one a tint shop hits most and it changes
        /// the work, not just the blame: the old film has to come off first.
        /// </summary>
        public static readonly string[] Choices =
        {
            Good,
            "Old film / residue",
            "Scratched",
            "Chipped",
            "Cracked",
            "Stained",
            NotApplicable
        };

        public static bool IsKnown(string? condition) =>
            condition != null && Choices.Contains(condition.Trim(), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A panel with a note against it but no condition still counts as
        /// looked at - somebody wrote something down about it.
        /// </summary>
        public static ConditionTone ToneOf(string? condition, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return string.IsNullOrWhiteSpace(notes) ? ConditionTone.NotRecorded : ConditionTone.Noted;

            var value = condition.Trim();

            if (string.Equals(value, Good, StringComparison.OrdinalIgnoreCase))
                return ConditionTone.Clear;

            if (string.Equals(value, NotApplicable, StringComparison.OrdinalIgnoreCase))
                return ConditionTone.NotApplicable;

            return IsKnown(value) ? ConditionTone.Issue : ConditionTone.Noted;
        }

        public static ConditionTone ToneOf(VehicleChecklistDetail? detail) =>
            detail == null ? ConditionTone.NotRecorded : ToneOf(detail.ExistingCondition, detail.Notes);

        /// <summary>
        /// The marker class for a panel's dot on the walkaround sheet. A
        /// panel the car does not have is not a warning - amber there read
        /// as something needing attention, when the answer is that there is
        /// nothing to attend to.
        /// </summary>
        public static string DotClass(ConditionTone tone) => tone switch
        {
            ConditionTone.Clear => "is-good",
            ConditionTone.Issue => "is-bad",
            ConditionTone.Noted => "is-info",
            ConditionTone.NotApplicable => "is-muted",
            _ => ""
        };
    }

    /// <summary>
    /// What one checklist found, counted up: how far through the walkaround
    /// it got and what it turned up. The list screen showed six columns of
    /// names and dates and nothing about the car, so a checklist with no
    /// panels recorded looked exactly like a finished one.
    /// </summary>
    public class ChecklistFindings
    {
        /// <summary>How many panels the walkaround asks about.</summary>
        public int Offered { get; private init; }

        public int Recorded { get; private init; }
        public int Clear { get; private init; }
        public int NotApplicable { get; private init; }
        public int Noted { get; private init; }
        public int Issues { get; private init; }

        /// <summary>The faults found, each named once, in walkaround order.</summary>
        public IReadOnlyList<string> IssueConditions { get; private init; } = Array.Empty<string>();

        public bool Started => Recorded > 0;
        public bool Complete => Offered > 0 && Recorded >= Offered;

        public static ChecklistFindings For(IEnumerable<VehicleChecklistDetail> details, int offered)
        {
            var rows = details
                .Select(d => (Detail: d, Tone: PanelConditions.ToneOf(d)))
                .Where(x => x.Tone != ConditionTone.NotRecorded)
                .ToList();

            return new ChecklistFindings
            {
                Offered = offered,
                Recorded = rows.Count,
                Clear = rows.Count(x => x.Tone == ConditionTone.Clear),
                NotApplicable = rows.Count(x => x.Tone == ConditionTone.NotApplicable),
                Noted = rows.Count(x => x.Tone == ConditionTone.Noted),
                Issues = rows.Count(x => x.Tone == ConditionTone.Issue),
                IssueConditions = rows
                    .Where(x => x.Tone == ConditionTone.Issue)
                    .Select(x => x.Detail.ExistingCondition!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => Array.FindIndex(PanelConditions.Choices,
                        choice => string.Equals(choice, c, StringComparison.OrdinalIgnoreCase)))
                    .ToList()
            };
        }

        /// <summary>
        /// One phrase for the list screen, following the finding rather than
        /// the kind of finding: damage outranks a note, a note outranks a
        /// clean car, and a sheet nobody has started says so.
        /// </summary>
        public string Headline =>
            !Started ? "Not started"
            : Issues > 0 ? $"{Issues} issue{(Issues == 1 ? "" : "s")}"
            : Noted > 0 ? $"{Noted} noted"
            : "Clear";

        public string HeadlineTone =>
            !Started ? "is-waiting"
            : Issues > 0 ? "is-bad"
            : Noted > 0 ? "is-info"
            : "is-good";
    }
}
