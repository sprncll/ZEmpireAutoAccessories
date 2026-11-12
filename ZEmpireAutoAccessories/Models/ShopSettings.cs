using System.ComponentModel.DataAnnotations;

namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// The handful of figures and names that were written into the code.
    ///
    /// The VAT rate was a const in <see cref="Vat"/>, the low-stock point was
    /// the number 5 written out in three separate files, and the shop's own
    /// name and address were a string literal in the PDF header. None of them
    /// is a decision a programmer should be making a year from now: BIR moves
    /// the rate, the shop moves premises, and what counts as "nearly out"
    /// differs between a roll of film and a box of horns.
    ///
    /// Stored as JSON beside the cut sizes rather than in a table, for the
    /// same reason they are: the database is the shop's and nothing may be
    /// added to it. See <c>Services/SettingsStore</c>.
    /// </summary>
    public class ShopSettings
    {
        [Required, MaxLength(120)]
        [Display(Name = "Shop name")]
        public string ShopName { get; set; } = "Z-Empire Auto Accessories";

        [MaxLength(120)]
        [Display(Name = "Tagline")]
        public string? Tagline { get; set; } = "Auto Tint & Accessories Shop";

        [MaxLength(200)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(60)]
        [Display(Name = "Contact number")]
        public string? ContactNumber { get; set; }

        /// <summary>
        /// The shop's TIN. A VAT-registered business prints it on every sales
        /// invoice and official receipt, and the PDFs could not before.
        /// </summary>
        [MaxLength(40)]
        [Display(Name = "TIN")]
        public string? TaxId { get; set; }

        /// <summary>
        /// VAT, as a percentage - 12 means 12%. Held as the figure people say
        /// rather than the fraction the arithmetic wants, because this is the
        /// box somebody types into.
        /// </summary>
        [Range(0, 100, ErrorMessage = "VAT must be between 0 and 100 percent.")]
        [Display(Name = "VAT rate (%)")]
        public decimal VatPercent { get; set; } = 12m;

        /// <summary>
        /// When a counted product is nearly out, in pieces.
        /// </summary>
        [Range(0, 100000, ErrorMessage = "The low-stock point can't be negative.")]
        [Display(Name = "Low stock: counted goods (pieces)")]
        public decimal LowStockPieces { get; set; } = 5m;

        /// <summary>
        /// When a roll is nearly out, in METRES - which is how the shop talks
        /// about film, and how the inventory list prints it.
        ///
        /// This is the one that was wrong. Stock is held in centimetres, so a
        /// single threshold of 5 meant five pieces of a horn and five
        /// CENTIMETRES of film: two inches, a roll already finished. Nothing
        /// showed as low until it was gone.
        /// </summary>
        [Range(0, 10000, ErrorMessage = "The low-stock point can't be negative.")]
        [Display(Name = "Low stock: film and tint (metres)")]
        public decimal LowStockMetres { get; set; } = 5m;

        /// <summary>The address block as the PDFs print it, or null when the shop has not said.</summary>
        public string? ContactLine =>
            string.Join("  ·  ", new[] { Address, ContactNumber, TaxId is { Length: > 0 } ? "TIN " + TaxId : null }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
