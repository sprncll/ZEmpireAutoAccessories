namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// A quotation has two states in this shop: it is being worked on, or it
    /// has become a job order. Sent, Accepted and Rejected were steps nobody
    /// filled in - the shop quotes a price, and either the customer says yes
    /// and the work is booked, or the quotation sits there - so they are gone
    /// from the screens.
    ///
    /// Nothing is removed from the database. CK_Quotation_Status still allows
    /// Draft, Sent, Accepted, Rejected, Converted, Cancelled and Expired, and
    /// quotations already carrying a retired status keep it; they simply read
    /// as Draft, which is what they are - not yet converted, still workable.
    /// Cancelled and Expired are left alone: folding those into Draft would
    /// show a quotation as live when somebody had put it away.
    ///
    /// Whether a quotation has been converted is read from the job order that
    /// was made from it, not from the status text. That is the fact of the
    /// matter, and it is right for the quotations converted before the status
    /// was being written at all.
    /// </summary>
    public static class QuotationStatuses
    {
        public const string Draft = "Draft";
        public const string Converted = "Converted";

        /// <summary>
        /// Statuses that stay as they are: a quotation somebody has put away
        /// should not read as though it were still open.
        /// </summary>
        private static readonly string[] Preserved = { "Cancelled", "Expired" };

        /// <summary>
        /// What to show for a quotation. A converted one says so; anything
        /// put away keeps its own word; everything else is a draft.
        /// </summary>
        /// <param name="converted">
        /// Whether a job order was made from it. Taken from the job order
        /// itself, not the status text, so the quotations converted before the
        /// status was written still read correctly.
        /// </param>
        public static string Display(string? status, bool converted) =>
            converted
                ? Converted
                : Preserved.FirstOrDefault(p =>
                      string.Equals(p, status, StringComparison.OrdinalIgnoreCase))
                  ?? Draft;

        public static string Display(Quotation quotation) =>
            Display(quotation.Status, quotation.JobOrder != null);

        /// <summary>
        /// The stored statuses a filter for <paramref name="status"/> should
        /// match. Asking for Draft has to pick up the retired ones too, or a
        /// report would leave out the very quotations the list is showing as
        /// drafts.
        /// </summary>
        public static string[] StoredValuesFor(string status) =>
            string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase)
                ? new[] { Draft, "Sent", "Accepted", "Rejected" }
                : new[] { status };

        /// <summary>The CSS modifier for that status's badge.</summary>
        public static string BadgeClass(string? status, bool converted) =>
            "badge-" + Display(status, converted).ToLowerInvariant();

        public static string BadgeClass(Quotation quotation) =>
            BadgeClass(quotation.Status, quotation.JobOrder != null);
    }
}
