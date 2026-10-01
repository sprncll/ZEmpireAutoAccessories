using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    // ===== schema: crm =====

    [Table("Customer", Schema = "crm")]
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(150, ErrorMessage = "Full name can't be longer than 150 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        // Deliberately no [Required] here even though new/edited customers
        // must supply one (enforced in CustomerController instead): the
        // database column is nullable and existing rows have NULL in it,
        // and [Required] makes EF Core's materializer assume the column is
        // never null and skip its DBNull check - which throws
        // SqlNullValueException the moment it reads one of those rows,
        // including through every list that Includes Customer.
        // Philippine mobile number, exactly as it is dialled: 11 digits
        // beginning 09, no spaces or dashes (e.g. 09121231231). Deliberately
        // not [Phone], which accepts spaces, dashes and country codes and
        // would let a number through in a shape this app does not want.
        // The column is nvarchar(30), so older rows may hold a longer or
        // differently formatted value; those only have to pass this on edit.
        [MaxLength(30, ErrorMessage = "Contact number can't be longer than 30 characters.")]
        [Display(Name = "Contact number")]
        [RegularExpression(@"^09\d{9}$",
            ErrorMessage = "Enter an 11-digit mobile number starting with 09 and no spaces, e.g. 09121231231.")]
        public string? ContactNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }

    [Table("VehicleClassification", Schema = "crm")]
    public class VehicleClassification
    {
        [Key]
        public int VehicleClassificationID { get; set; }

        [Required, MaxLength(80)]
        public string ClassificationName { get; set; } = string.Empty;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }

    [Table("Vehicle", Schema = "crm")]
    public class Vehicle
    {
        [Key]
        public int VehicleID { get; set; }

        [Display(Name = "Customer")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a customer.")]
        public int CustomerID { get; set; }

        [Display(Name = "Classification")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a classification.")]
        public int VehicleClassificationID { get; set; }

        // At most three letters and at most four numbers, in either order.
        //
        // Letters first is the car plate - ABC 1234, and the older ABC 123.
        // Numbers first is the motorcycle plate - 295 XBB. The old rule
        // demanded exactly three letters and put them first, which refused
        // every motorcycle in the yard, including one already on file.
        [MaxLength(8, ErrorMessage = "A plate number is at most three letters and four numbers.")]
        [RegularExpression(@"^([A-Za-z]{1,3}[- ]?\d{1,4}|\d{1,4}[- ]?[A-Za-z]{1,3})$",
            ErrorMessage = "Enter a valid Philippine plate number - up to three letters and four numbers, like ABC 1234, ABC 123 or 295 XBB.")]
        [Display(Name = "Plate number")]
        public string? PlateNumber { get; set; }

        [MaxLength(60, ErrorMessage = "Brand can't be longer than 60 characters.")]
        [Display(Name = "Brand")]
        public string? Brand { get; set; }

        [MaxLength(60, ErrorMessage = "Model can't be longer than 60 characters.")]
        [Display(Name = "Model")]
        public string? Model { get; set; }

        [Display(Name = "Year")]
        [Range(1900, 2100, ErrorMessage = "Enter a valid year (1900–2100).")]
        public short? ManufacturingYear { get; set; }

        public Customer Customer { get; set; } = null!;
        public VehicleClassification VehicleClassification { get; set; } = null!;
    }

    // Reference guide only - which classification a given brand/model
    // normally falls under. Used to help staff pick the right
    // VehicleClassification when registering a vehicle; not linked to any
    // specific Vehicle record.
    [Table("VehicleModelGuide", Schema = "crm")]
    public class VehicleModelGuide
    {
        [Key]
        public int VehicleModelGuideID { get; set; }

        [Required, MaxLength(60)]
        public string Brand { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        [Display(Name = "Model")]
        public string ModelName { get; set; } = string.Empty;

        public int VehicleClassificationID { get; set; }

        public VehicleClassification VehicleClassification { get; set; } = null!;
    }
}
