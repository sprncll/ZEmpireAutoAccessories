using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZEmpireAutoAccessories.Models
{
    // ===== schema: inv =====

    [Table("InventoryTransaction", Schema = "inv")]
    public class InventoryTransaction
    {
        [Key]
        public int InventoryTransactionID { get; set; }

        public int ProductID { get; set; }
        public string UserId { get; set; } = null!;

        [Required, MaxLength(3)]
        public string TransactionType { get; set; } = string.Empty; // 'IN' or 'OUT'

        public decimal Quantity { get; set; }
        public DateTime TransactionDate { get; set; }

        public Product Product { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }

    /// <summary>
    /// A row of the Inventory list: what is on hand, in the unit the product
    /// is measured in, with the category it is grouped under.
    /// </summary>
    public class InventoryRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal StockOnHand { get; set; }
        public bool SoldByLength { get; set; }
    }

    /// <summary>One row of a stock count sheet: what the system holds, ready for a shelf figure.</summary>
    public class StockCountRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal SystemStock { get; set; }

        /// <summary>Roll goods: counted in cm/in/m rather than pieces.</summary>
        public bool SoldByLength { get; set; }

        /// <summary>
        /// The shade this line counts, where the product is broken down.
        /// inv.InventoryCheckDetail has carried TintVariantID and ShadeID all
        /// along, so a count CAN be recorded per shade even though the running
        /// balance cannot: inv.InventoryTransaction records a product and
        /// nothing finer. Null on both means the line counts the product whole,
        /// which is every piece good and every film with no shades on file.
        /// </summary>
        public int? TintVariantID { get; set; }
        public int? ShadeID { get; set; }

        public string? TintVariantName { get; set; }
        public string? ShadeName { get; set; }

        /// <summary>Whether this line counts one shade rather than the product.</summary>
        public bool IsShadeLine => ShadeID != null;

        /// <summary>
        /// Whether this is the first line of its product, which is where the
        /// system figure belongs. The system knows one number per product, so
        /// repeating it beside every shade would read as though each shade had
        /// that much.
        /// </summary>
        public bool FirstOfProduct { get; set; }

        /// <summary>How many lines this product is split across.</summary>
        public int ShadeLineCount { get; set; } = 1;

        /// <summary>"BF Stone - Superdark", or the product name when whole.</summary>
        public string Label =>
            IsShadeLine
                ? string.Join(" - ", new[] { TintVariantName, ShadeName }
                      .Where(x => !string.IsNullOrWhiteSpace(x)))
                : ProductName;
    }

    /// <summary>
    /// A counted line against stock-on-hand as it stands now. Positive
    /// variance means the shelf holds more than the system thought.
    /// </summary>
    public class StockVarianceRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal PhysicalStock { get; set; }
        public decimal SystemStock { get; set; }
        public string Unit { get; set; } = "Piece";
        public string StockLevel { get; set; } = "Normal";
        public bool SoldByLength { get; set; }

        /// <summary>
        /// What was counted against each shade, where the product was counted
        /// that way. The variance itself stays at product level - there is one
        /// system figure per product to compare against, and reconciling posts
        /// to inv.InventoryTransaction, which has no shade. This is the
        /// breakdown behind the total, for reading.
        /// </summary>
        public List<StockCountShade> Shades { get; set; } = new();

        public decimal Variance => PhysicalStock - SystemStock;
    }

    /// <summary>One shade's share of a counted product.</summary>
    public class StockCountShade
    {
        public string Label { get; set; } = string.Empty;
        public decimal PhysicalStock { get; set; }
    }

    /// <summary>
    /// One product line of a document (job order, service invoice) whose
    /// completion moves stock. Not a mapped entity - inv.InventoryTransaction
    /// records only the movement itself, with no link back to what caused it.
    /// </summary>
    /// <param name="Unit">
    /// The unit Quantity was entered in ("cm", "in", "m"). Null means the
    /// quantity is already in the product's stock unit, which is how the
    /// stock-count reconcile and every piece-goods caller pass it.
    /// </param>
    public record DocumentStockLine(int ProductID, decimal Quantity, string? Unit = null);

    [Table("InventoryCheck", Schema = "inv")]
    public class InventoryCheck
    {
        [Key]
        public int InventoryCheckID { get; set; }

        public string UserId { get; set; } = null!;
        public DateTime CheckDate { get; set; }

        public ApplicationUser User { get; set; } = null!;
        public ICollection<InventoryCheckDetail> Details { get; set; } = new List<InventoryCheckDetail>();
    }

    [Table("InventoryCheckDetail", Schema = "inv")]
    public class InventoryCheckDetail
    {
        [Key]
        public int InventoryCheckDetailID { get; set; }

        public int InventoryCheckID { get; set; }
        public int ProductID { get; set; }
        public int? TintVariantID { get; set; }
        public int? ShadeID { get; set; }

        public decimal PhysicalStock { get; set; }

        [Required, MaxLength(10)]
        public string StockLevel { get; set; } = "Normal"; // Normal / Low / Critical

        [Required, MaxLength(10)]
        public string Unit { get; set; } = string.Empty; // Piece / Roll

        public InventoryCheck InventoryCheck { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public TintVariant? TintVariant { get; set; }
        public Shade? Shade { get; set; }
    }
}
