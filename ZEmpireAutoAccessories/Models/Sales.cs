using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    // ===== schema: sales =====

    [Table("PaymentMode", Schema = "sales")]
    public class PaymentMode
    {
        [Key]
        public int PaymentModeID { get; set; }

        [Required, MaxLength(50)]
        public string PaymentModeName { get; set; } = string.Empty;
    }

    [Table("Sales", Schema = "sales")]
    public class Sale
    {
        [Key]
        public int SalesID { get; set; }

        [Required, MaxLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public int CustomerID { get; set; }

        [Display(Name = "Vehicle")]
        public int? VehicleID { get; set; }

        public string UserId { get; set; } = null!;

        [Display(Name = "Payment mode")]
        public int PaymentModeID { get; set; }

        public DateTime SalesDate { get; set; }
        public decimal TotalAmount { get; set; }

        public Customer Customer { get; set; } = null!;
        public Vehicle? Vehicle { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public PaymentMode PaymentMode { get; set; } = null!;
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    }

    [Table("SalesDetail", Schema = "sales")]
    public class SaleDetail
    {
        [Key]
        public int SalesDetailID { get; set; }

        public int SalesID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Which shade of the film this line took off the roll, where the
        /// line is film at all.
        ///
        /// Nullable, and null for most of the catalogue: a bolt and a step
        /// board have no shade. Null also on every sale written before the
        /// column existed - nobody knows which shade those took, and a guess
        /// would be worse than a blank.
        ///
        /// The variant is not stored beside it. A shade belongs to exactly
        /// one tint variant, so Shade.TintVariant is the answer and a second
        /// column would be a second place for it to be wrong.
        ///
        /// Database/AddShadeToSalesDetail.sql adds the column.
        /// </summary>
        public int? ShadeID { get; set; }

        // Persisted computed column (Quantity * UnitPrice)
        public decimal SubTotal { get; private set; }

        public Sale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Shade? Shade { get; set; }
    }

    [Table("ServiceInvoice", Schema = "sales")]
    public class ServiceInvoice
    {
        [Key]
        public int ServiceInvoiceID { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int InvoiceNoSeriesID { get; set; }
        public int? JobOrderID { get; set; }

        [Display(Name = "Customer")]
        public int CustomerID { get; set; }

        [Display(Name = "Vehicle")]
        public int? VehicleID { get; set; }

        public string UserId { get; set; } = null!;

        [Display(Name = "Payment mode")]
        public int PaymentModeID { get; set; }

        public DateTime InvoiceDate { get; set; }

        // SubTotal, TotalAmount and ChangeAmount are computed by
        // RecalculateTotals, never typed in. The three that are typed in each
        // back a >= 0 CHECK constraint (CK_ServiceInvoice_Discount, _Tax and
        // _AmountPaid), so catch a negative here rather than at the database.
        public decimal SubTotal { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Discount can't be negative.")]
        [Display(Name = "Discount")]
        public decimal DiscountAmount { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Tax can't be negative.")]
        [Display(Name = "Tax")]
        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Amount paid can't be negative.")]
        [Display(Name = "Amount paid")]
        public decimal AmountPaid { get; set; }

        public decimal ChangeAmount { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Paid";

        public DateTime CreatedAt { get; set; }

        public InvoiceNoSeries InvoiceNoSeries { get; set; } = null!;
        public JobOrder? JobOrder { get; set; }
        public Customer Customer { get; set; } = null!;
        public Vehicle? Vehicle { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public PaymentMode PaymentMode { get; set; } = null!;
        public ICollection<ServiceInvoiceDetail> Details { get; set; } = new List<ServiceInvoiceDetail>();
    }

    [Table("ServiceInvoiceDetail", Schema = "sales")]
    public class ServiceInvoiceDetail
    {
        [Key]
        public int ServiceInvoiceDetailID { get; set; }

        public int ServiceInvoiceID { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public int? TintVariantID { get; set; }
        public int? ShadeID { get; set; }
        public int? PanelID { get; set; }

        [Required, MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        [Required, MaxLength(20)]
        public string Unit { get; set; } = "Unit";

        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }

        // Persisted computed column (Quantity * UnitPrice - DiscountAmount)
        public decimal SubTotal { get; private set; }

        public ServiceInvoice ServiceInvoice { get; set; } = null!;
        public Product? Product { get; set; }
        public Service? Service { get; set; }
        public TintVariant? TintVariant { get; set; }
        public Shade? Shade { get; set; }
        public Panel? Panel { get; set; }
    }

    [Table("Quotation", Schema = "sales")]
    public class Quotation
    {
        [Key]
        public int QuotationID { get; set; }

        [Required, MaxLength(30)]
        public string QuotationNumber { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public int CustomerID { get; set; }

        [Display(Name = "Vehicle")]
        public int VehicleID { get; set; }

        public string UserId { get; set; } = null!;
        public int? JobTypeID { get; set; }

        public DateTime QuotationDate { get; set; }
        public DateOnly? ValidUntil { get; set; }

        // As on ServiceInvoice: SubTotal and TotalAmount are computed, and
        // the two that are typed in each back a >= 0 CHECK constraint
        // (CK_Quotation_Discount, CK_Quotation_Tax).
        public decimal SubTotal { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Discount can't be negative.")]
        [Display(Name = "Discount")]
        public decimal DiscountAmount { get; set; }

        [Range(0, 9999999999.99, ErrorMessage = "Tax can't be negative.")]
        [Display(Name = "Tax")]
        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; }

        public Customer Customer { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public JobType? JobType { get; set; }
        public ICollection<QuotationDetail> Details { get; set; } = new List<QuotationDetail>();

        // Set once the quote is converted (JobOrder.QuotationID points back here)
        public JobOrder? JobOrder { get; set; }
    }

    [Table("QuotationDetail", Schema = "sales")]
    public class QuotationDetail
    {
        [Key]
        public int QuotationDetailID { get; set; }

        public int QuotationID { get; set; }
        public int? ProductID { get; set; }
        public int? ServiceID { get; set; }
        public int? TintVariantID { get; set; }
        public int? ShadeID { get; set; }
        public int? PanelID { get; set; }
        public int? PricingID { get; set; }

        [MaxLength(250)]
        public string? Description { get; set; }

        public int Quantity { get; set; }

        [Required, MaxLength(20)]
        public string Unit { get; set; } = "Unit";

        public decimal UnitPrice { get; set; }

        // Persisted computed column (Quantity * UnitPrice)
        public decimal SubTotal { get; private set; }

        public Quotation Quotation { get; set; } = null!;
        public Product? Product { get; set; }
        public Service? Service { get; set; }
        public TintVariant? TintVariant { get; set; }
        public Shade? Shade { get; set; }
        public Panel? Panel { get; set; }
        public Pricing? Pricing { get; set; }
    }

    [Table("InvoiceNoSeries", Schema = "sales")]
    public class InvoiceNoSeries
    {
        [Key]
        public int InvoiceNoSeriesID { get; set; }

        public string UserId { get; set; } = null!;
        public int SeriesYear { get; set; }

        [Required, MaxLength(20)]
        public string Prefix { get; set; } = "INV";

        public int CurrentNumber { get; set; }
        public int NumberLength { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }

    [Table("Warranty", Schema = "sales")]
    public class Warranty
    {
        [Key]
        public int WarrantyID { get; set; }

        public int? SalesDetailID { get; set; }
        public int? ServiceInvoiceDetailID { get; set; }
        public int? JobOrderID { get; set; }

        [Display(Name = "Warranty starts")]
        public DateOnly? WarrantyStartDate { get; set; }
        [Display(Name = "Warranty ends")]
        public DateOnly? WarrantyEndDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "Terms")]
        public string? WarrantyTerms { get; set; }

        [MaxLength(100)]
        [Display(Name = "Status")]
        public string? WarrantyStatus { get; set; }

        [MaxLength(150)]
        public string? Remarks { get; set; }

        [MaxLength(150)]
        public string? CreatedBy { get; set; }

        public SaleDetail? SalesDetail { get; set; }
        public ServiceInvoiceDetail? ServiceInvoiceDetail { get; set; }
        public JobOrder? JobOrder { get; set; }
    }
}
