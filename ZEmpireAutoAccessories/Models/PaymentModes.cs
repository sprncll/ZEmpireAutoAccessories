namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The ways this shop takes money, and which of them arrive from somewhere
    /// other than the counter.
    ///
    /// Cash and a card terminal are settled in front of you - the money is in
    /// the drawer or the terminal has printed a slip. A bank transfer or a
    /// QRPH payment is a claim until someone checks it: the customer says they
    /// sent it, and the only thing tying the transaction to the money is the
    /// screenshot or receipt they show. Those modes are marked here so
    /// Cashiering can ask for that proof and flag the rows still missing one.
    ///
    /// Both lists can be replaced from appsettings.json, so a new wallet or
    /// rail is a settings change rather than a code change - and never a
    /// schema change. sales.PaymentMode holds the rows; PaymentModeSeeder puts
    /// any missing name there at startup.
    /// </summary>
    public static class PaymentModes
    {
        /// <summary>
        /// The modes the shop offers. Order is the order they are shown in.
        /// </summary>
        private static readonly string[] DefaultOffered =
        {
            "Cash",
            "Card",
            "Bank Transfer",
            "QRPH"
        };

        /// <summary>
        /// Modes where the money moves before anyone at the shop sees it, so a
        /// receipt or screenshot is what proves it arrived. GCash is here
        /// because the existing data uses it; QRPH is the rail that replaced
        /// it for most counters.
        /// </summary>
        private static readonly string[] DefaultRequiringProof =
        {
            "Bank Transfer",
            "QRPH",
            "GCash"
        };

        private static string[] Offered = DefaultOffered;

        private static HashSet<string> RequiringProof =
            new(DefaultRequiringProof, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Points both lists at configuration. An absent or empty setting
        /// keeps the built-in list, so a missing section can never leave the
        /// shop with no way to take money.
        /// </summary>
        public static void Configure(IEnumerable<string>? offered, IEnumerable<string>? requiringProof)
        {
            var cleanOffered = Clean(offered);
            if (cleanOffered.Count > 0)
                Offered = cleanOffered.ToArray();

            var cleanProof = Clean(requiringProof);
            if (cleanProof.Count > 0)
                RequiringProof = new HashSet<string>(cleanProof, StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> Clean(IEnumerable<string>? names) =>
            names?.Where(n => !string.IsNullOrWhiteSpace(n))
                  .Select(n => n.Trim())
                  .ToList()
            ?? new List<string>();

        /// <summary>The modes this shop offers, in display order.</summary>
        public static IReadOnlyList<string> OfferedNames => Offered;

        /// <summary>
        /// Whether a payment on this mode needs a receipt attached to it.
        /// </summary>
        public static bool NeedsProof(string? paymentModeName) =>
            paymentModeName != null && RequiringProof.Contains(paymentModeName.Trim());

        /// <summary>
        /// What a form should offer, from the rows the table actually holds:
        /// the offered modes in the order set above, and nothing else -
        /// except the mode a document is already on, which stays on the list
        /// so opening an old GCash sale and saving it does not silently move
        /// it to another mode.
        ///
        /// A mode is retired by leaving it off the list, never by deleting
        /// its row: every transaction that used it still points there.
        /// </summary>
        public static List<PaymentMode> ForSelection(IEnumerable<PaymentMode> all, int? keepId = null)
        {
            var order = Offered
                .Select((name, index) => (name, index))
                .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

            return all
                .Where(mode => order.ContainsKey(mode.PaymentModeName)
                               || (keepId.HasValue && mode.PaymentModeID == keepId.Value))
                // Retired-but-kept modes sort last, after everything offered.
                .OrderBy(mode => order.TryGetValue(mode.PaymentModeName, out var index)
                    ? index
                    : int.MaxValue)
                .ThenBy(mode => mode.PaymentModeName)
                .ToList();
        }
    }
}
