using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// One movement of money against a service invoice: a down payment, the
    /// balance, a refund.
    ///
    /// sales.ServiceInvoice holds one running AmountPaid, which says how much
    /// has come in but not when, by what, or to whom. So the day's takings
    /// could only be counted by invoice - an invoice counted once it was Paid,
    /// in full, on the day it was raised. A P30,000 deposit taken today did
    /// not appear in today's drawer at all, and turned up a week later inside
    /// a P67,000 figure dated last week.
    ///
    /// Each payment is its own row now, dated when it was taken, by the mode
    /// it came in on and the cashier who took it, and the takings are the sum
    /// of these. AmountPaid on the invoice is still kept, as the running total
    /// the rest of the system already reads.
    ///
    /// A refund or a reversal is a row with a negative amount, dated when the
    /// money went back - so the day it was paid keeps its takings and the day
    /// it was returned shows the return, which is what the drawer did.
    /// </summary>
    [Table("ServiceInvoicePayment", Schema = "sales")]
    public class ServiceInvoicePayment
    {
        public const string KindPayment = "Payment";
        public const string KindRefund = "Refund";
        public const string KindReversal = "Reversal";

        [Key]
        public int PaymentID { get; set; }

        public int ServiceInvoiceID { get; set; }

        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// What this movement put into the drawer, net of change: positive for
        /// a payment, negative for money given back.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>What the customer handed over. Null on a refund.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AmountTendered { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }

        public int PaymentModeID { get; set; }

        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Payment, Refund or Reversal.</summary>
        [Required, MaxLength(20)]
        public string Kind { get; set; } = KindPayment;

        [MaxLength(200)]
        public string? Notes { get; set; }

        public ServiceInvoice ServiceInvoice { get; set; } = null!;
        public PaymentMode PaymentMode { get; set; } = null!;
    }
}
