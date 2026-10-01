using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    // ===== schema: ops =====

    [Table("JobOrder", Schema = "ops")]
    public class JobOrder
    {
        [Key]
        public int JobOrderID { get; set; }

        [Required, MaxLength(30)]
        public string JobOrderNumber { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public int CustomerID { get; set; }

        [Display(Name = "Vehicle")]
        public int VehicleID { get; set; }

        public string UserId { get; set; } = null!;
        public int? JobTypeID { get; set; }
        public int? AssignedEmployeeID { get; set; }
        public int? QuotationID { get; set; }

        public DateTime JobOrderDate { get; set; }
        public DateTime? InstallationDate { get; set; }

        [MaxLength(100)]
        public string? ExistingFilmShade { get; set; }

        [MaxLength(400)]
        public string? ReasonForChanging { get; set; }

        [MaxLength(400)]
        public string? SpecialInstruction { get; set; }

        [MaxLength(400)]
        public string? Complaint { get; set; }

        // Nothing asks for this any more - it is off the job order forms, the
        // details screen and the printed job order. The property stays because
        // the column does: ops.JobOrder.Odometer is NOT NULL-able but present,
        // and dropping it is a schema change. Existing readings are left
        // exactly as they were.
        public int? Odometer { get; set; }

        public string? ClientSignature { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public Customer Customer { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public JobType? JobType { get; set; }
        public Employee? AssignedEmployee { get; set; }
        public Quotation? Quotation { get; set; }
        public ICollection<JobOrderDetail> Details { get; set; } = new List<JobOrderDetail>();

        // A job order can be invoiced once it's Completed/Posted (ServiceInvoice.JobOrderID points back here).
        public ICollection<ServiceInvoice> ServiceInvoices { get; set; } = new List<ServiceInvoice>();
    }

    [Table("JobOrderDetail", Schema = "ops")]
    public class JobOrderDetail
    {
        [Key]
        public int JobOrderDetailID { get; set; }

        public int JobOrderID { get; set; }
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

        public JobOrder JobOrder { get; set; } = null!;
        public Product? Product { get; set; }
        public Service? Service { get; set; }
        public TintVariant? TintVariant { get; set; }
        public Shade? Shade { get; set; }
        public Panel? Panel { get; set; }
        public Pricing? Pricing { get; set; }
    }

    [Table("VehicleChecklist", Schema = "ops")]
    public class VehicleChecklist
    {
        [Key]
        public int ChecklistID { get; set; }

        public int JobOrderID { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime ChecklistDate { get; set; }

        [MaxLength(400)]
        public string? AdditionalNotes { get; set; }

        public string? ClientSignature { get; set; }

        public JobOrder JobOrder { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public ICollection<VehicleChecklistDetail> Details { get; set; } = new List<VehicleChecklistDetail>();
    }

    [Table("VehicleChecklistDetail", Schema = "ops")]
    public class VehicleChecklistDetail
    {
        [Key]
        public int VehicleChecklistDetailID { get; set; }

        public int ChecklistID { get; set; }
        public int PanelID { get; set; }

        [MaxLength(200)]
        public string? ExistingCondition { get; set; }

        [MaxLength(400)]
        public string? Notes { get; set; }

        public VehicleChecklist Checklist { get; set; } = null!;
        public Panel Panel { get; set; } = null!;
    }
}
