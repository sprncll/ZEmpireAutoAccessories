namespace ZEmpireAutoAccessories.Models
{
    // ===== view models for the Cashiering module =====
    // Cashiering has no tables of its own. It is a read-only view over the
    // two things this shop actually collects money for: product sales
    // (sales.Sales) and service invoices (sales.ServiceInvoice). Both carry
    // an invoice number, a customer, a payment mode and a recording user,
    // so they are projected onto one row shape and merged.

    /// <summary>Which table a cashier row came from.</summary>
    public enum CashierSource
    {
        Sale = 0,
        ServiceInvoice = 1
    }

    /// <summary>
    /// One collected transaction, projected from either sales.Sales or
    /// sales.ServiceInvoice.
    /// </summary>
    public class CashierTransaction
    {
        public CashierSource Source { get; set; }

        /// <summary>SalesID or ServiceInvoiceID, depending on Source.</summary>
        public int SourceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }

        // Nullable because crm.Customer.FullName is a nullable column with
        // pre-existing NULL rows - see the Customer mapping in
        // ApplicationDbContext for the full story.
        public string? CustomerName { get; set; }

        public string? PlateNumber { get; set; }

        public int PaymentModeID { get; set; }
        public string PaymentModeName { get; set; } = string.Empty;

        /// <summary>asp.AspNetUsers.Id of the user who recorded the transaction.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Display name of that user, falling back to their login.</summary>
        public string? CashierName { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Service invoices record tendered cash and change; product sales do
        /// not have those columns, so both are null for a Sale row.
        /// </summary>
        public decimal? AmountPaid { get; set; }
        public decimal? ChangeAmount { get; set; }

        /// <summary>
        /// sales.ServiceInvoice.Status (Pending / Paid / Cancelled / Refunded).
        /// sales.Sales has no status column - a recorded product sale is
        /// always a completed one - so Sale rows report "Paid".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Whether this row counts towards the drawer total. Service invoices
        /// only count once they are actually Paid; Pending, Cancelled and
        /// Refunded invoices still appear in the list but are not collected
        /// cash.
        /// </summary>
        public bool IsCollected => Source == CashierSource.Sale || Status == "Paid";

        /// <summary>
        /// Whether a receipt for this payment has been attached. Set by the
        /// controller from the proof folder - there is no column for it, and
        /// none is wanted: see PaymentProofStore.
        /// </summary>
        public bool HasProof { get; set; }

        /// <summary>
        /// Whether this payment arrived from somewhere other than the counter,
        /// so a receipt is what shows the money came in.
        /// </summary>
        public bool NeedsProof => PaymentModes.NeedsProof(PaymentModeName);

        /// <summary>
        /// Collected on an online mode with nothing attached to show for it -
        /// the row a cashier has to chase before closing the day.
        /// </summary>
        public bool ProofMissing => NeedsProof && IsCollected && !HasProof;
    }

    /// <summary>Collected cash for one payment mode within the filtered range.</summary>
    public class CashierPaymentModeTotal
    {
        public int PaymentModeID { get; set; }
        public string PaymentModeName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Collected cash for one cashier within the filtered range.</summary>
    public class CashierUserTotal
    {
        public string UserId { get; set; } = string.Empty;
        public string? CashierName { get; set; }
        public int TransactionCount { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Everything the Cashiering index screen renders.</summary>
    public class CashieringViewModel
    {
        public List<CashierTransaction> Transactions { get; set; } = new();

        // ----- filters echoed back to the form -----
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public string? Search { get; set; }
        public int? PaymentModeID { get; set; }
        public string? UserId { get; set; }
        public CashierSource? Source { get; set; }

        // ----- totals over the filtered set -----
        public decimal SalesTotal { get; set; }
        public decimal ServiceInvoiceTotal { get; set; }
        public decimal GrandTotal { get; set; }

        /// <summary>Rows in range that are not collected cash (unpaid, cancelled, refunded).</summary>
        public decimal UncollectedTotal { get; set; }

        public int CollectedCount { get; set; }
        public int UncollectedCount { get; set; }

        public List<CashierPaymentModeTotal> ByPaymentMode { get; set; } = new();
        public List<CashierUserTotal> ByCashier { get; set; } = new();

        // ----- what this user is allowed to see -----
        // Cashiering has no module of its own; it is opened by holding Sales
        // or Service Invoices, and shows only the half that is held. The view
        // uses these to hide the tiles, filters and links for the other half.
        public bool CanSeeSales { get; set; }
        public bool CanSeeServiceInvoices { get; set; }

        /// <summary>
        /// Whether the day's takings may be shown. Admin only, matching the
        /// dashboard, where peso figures are already withheld from Staff. A
        /// cashier still gets the transaction list - they need it to work the
        /// counter - but not the totals it rolls up to. When this is false
        /// the controller leaves the totals below at zero rather than
        /// trusting the view to hide them.
        /// </summary>
        public bool CanSeeTotals { get; set; }
    }
}
