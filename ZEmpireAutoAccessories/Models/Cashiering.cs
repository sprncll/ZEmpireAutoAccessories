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
        public bool IsCollected => FromLedger
            ? Source == CashierSource.Sale || PaymentId != null
            : Source == CashierSource.Sale || Status == "Paid";

        /// <summary>
        /// Read from the payment ledger (sales.ServiceInvoicePayment): each
        /// invoice row is one payment, dated when it was taken, and its amount
        /// is what that payment put in the drawer. False until the ledger's
        /// script has been run, when a row is a whole invoice as before.
        /// </summary>
        public bool FromLedger { get; set; }

        /// <summary>The payment this row is, on an invoice row from the ledger.</summary>
        public int? PaymentId { get; set; }

        /// <summary>
        /// The invoice's own total, where the row's amount is only part of it -
        /// a down payment, a balance, a refund.
        /// </summary>
        public decimal? DocumentTotal { get; set; }

        /// <summary>"Down payment", "Balance", "Refunded", "Balance due"...</summary>
        public string? PaymentLabel { get; set; }

        /// <summary>
        /// The receipts attached to this payment, by number, in the order
        /// they were added - a down payment and its balance are two. Set by
        /// the controller from the proof folder - there is no column for it,
        /// and none is wanted: see ProofStore.
        /// </summary>
        public IReadOnlyList<int> ProofSlots { get; set; } = Array.Empty<int>();

        /// <summary>Whether any receipt for this payment has been attached.</summary>
        public bool HasProof => ProofSlots.Count > 0;

        /// <summary>
        /// Whether this payment left a record worth attaching - a transfer
        /// screenshot, a wallet receipt, a card terminal's slip. Only cash
        /// has nothing behind it.
        /// </summary>
        public bool NeedsProof => PaymentModes.NeedsProof(PaymentModeName);

        /// <summary>
        /// Collected on an online mode with nothing attached to show for it -
        /// the row a cashier has to chase before closing the day.
        /// </summary>
        public bool ProofMissing => NeedsProof && IsCollected && TotalAmount > 0m && !HasProof;
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
        /// <summary>
        /// Invoice money is counted from the payment ledger - by the day each
        /// payment was taken - rather than by whole invoice.
        /// </summary>
        public bool UsesLedger { get; set; }

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
