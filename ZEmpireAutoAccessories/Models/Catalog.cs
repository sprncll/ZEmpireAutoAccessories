using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    // ===== schema: cat =====

    [Table("ProductCategory", Schema = "cat")]
    public class ProductCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required, MaxLength(80)]
        public string CategoryName { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("Product", Schema = "cat")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        public int CategoryID { get; set; }

        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Description { get; set; }

        // CK_Product_DefaultPrice allows NULL or >= 0. Catch it here so a
        // negative price comes back as a field error rather than a raw SQL
        // rejection on save.
        [Range(0, 9999999999.99, ErrorMessage = "Default price can't be negative.")]
        public decimal? DefaultPrice { get; set; }

        public bool IsActive { get; set; }

        public ProductCategory Category { get; set; } = null!;
        public ICollection<Pricing> Pricings { get; set; } = new List<Pricing>();
        public ICollection<TintVariant> TintVariants { get; set; } = new List<TintVariant>();
    }

    [Table("TintVariant", Schema = "cat")]
    public class TintVariant
    {
        [Key]
        public int TintVariantID { get; set; }

        public int ProductID { get; set; }

        [Required, MaxLength(120)]
        public string VariantName { get; set; } = string.Empty;

        public Product Product { get; set; } = null!;
        public ICollection<Shade> Shades { get; set; } = new List<Shade>();
    }

    [Table("Shade", Schema = "cat")]
    public class Shade
    {
        [Key]
        public int ShadeID { get; set; }

        public int TintVariantID { get; set; }

        [Required, MaxLength(60)]
        public string ShadeName { get; set; } = string.Empty;

        public TintVariant TintVariant { get; set; } = null!;
    }

    [Table("Panel", Schema = "cat")]
    public class Panel
    {
        [Key]
        public int PanelID { get; set; }

        [Required, MaxLength(100)]
        public string PanelName { get; set; } = string.Empty;
    }

    [Table("ServiceCategory", Schema = "cat")]
    public class ServiceCategory
    {
        [Key]
        public int ServiceCategoryID { get; set; }

        [Required, MaxLength(80)]
        public string CategoryName { get; set; } = string.Empty;

        public ICollection<Service> Services { get; set; } = new List<Service>();
    }

    [Table("Service", Schema = "cat")]
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        public int ServiceCategoryID { get; set; }

        [Required, MaxLength(150)]
        public string ServiceName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Description { get; set; }

        // CK_Service_Price backs this with >= 0 at the database level.
        [Range(0, 9999999999.99, ErrorMessage = "Default price can't be negative.")]
        public decimal DefaultPrice { get; set; }

        public bool IsActive { get; set; }

        public ServiceCategory ServiceCategory { get; set; } = null!;
    }

    [Table("JobType", Schema = "cat")]
    public class JobType
    {
        [Key]
        public int JobTypeID { get; set; }

        [Required, MaxLength(80)]
        public string JobTypeName { get; set; } = string.Empty;
    }

    [Table("Pricing", Schema = "cat")]
    public class Pricing
    {
        [Key]
        public int PricingID { get; set; }

        public int ProductID { get; set; }
        public int? TintVariantID { get; set; }
        public int VehicleClassificationID { get; set; }
        public int PanelID { get; set; }

        public decimal Price { get; set; }

        public Product Product { get; set; } = null!;
        public TintVariant? TintVariant { get; set; }
        public VehicleClassification VehicleClassification { get; set; } = null!;
        public Panel Panel { get; set; } = null!;
        public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
    }

    [Table("PriceHistory", Schema = "cat")]
    public class PriceHistory
    {
        [Key]
        public int HistoryID { get; set; }

        public int PricingID { get; set; }
        public string UserId { get; set; } = null!;

        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public DateTime DateChanged { get; set; }

        public Pricing Pricing { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
