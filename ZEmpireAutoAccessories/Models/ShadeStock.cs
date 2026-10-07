namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// How much of each shade is left, worked out rather than recorded.
    ///
    /// inv.InventoryTransaction carries a ProductID and nothing finer, so the
    /// running balance cannot answer "how much Superdark is on the rack" and
    /// no amount of reading it will. But two things that CAN be broken down by
    /// shade now exist: a stock count records what was physically on the rack
    /// per shade, and every document line records which shade was fitted. One
    /// gives a starting point, the other gives what has gone since.
    ///
    ///     left = counted at the last stock count - used on documents since
    ///
    /// That is an estimate, and it is only as good as its inputs: a film line
    /// with no shade on it cannot be subtracted from any shade, and a delivery
    /// cannot be added to one. Both are surfaced rather than swallowed, and
    /// the product's real ledger figure is shown beside the shades so the two
    /// can be compared. It is not a substitute for counting; it is what keeps
    /// the last count useful until the next one.
    /// </summary>
    public class ShadeStockRow
    {
        public int TintVariantID { get; set; }
        public int ShadeID { get; set; }
        public string TintVariantName { get; set; } = string.Empty;
        public string ShadeName { get; set; } = string.Empty;

        /// <summary>What the last count that covered this shade found, in centimetres.</summary>
        public decimal? Counted { get; set; }

        /// <summary>When that count was taken.</summary>
        public DateTime? CountedOn { get; set; }

        /// <summary>Centimetres fitted on documents dated after that count.</summary>
        public decimal Used { get; set; }

        /// <summary>
        /// What should be left. Null where the shade has never been counted -
        /// there is no starting point, and showing a bare negative of what has
        /// been used would read as a figure rather than as the absence of one.
        /// </summary>
        public decimal? Left => Counted == null ? null : Counted - Used;

        /// <summary>"BF Stone - Superdark".</summary>
        public string Label =>
            string.Join(" - ", new[] { TintVariantName, ShadeName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        /// <summary>Nothing left, or into the negative - which means a miscount or a missing line.</summary>
        public bool Exhausted => Left is <= 0m;
    }

    /// <summary>One roll product, with its shades and everything that qualifies the figures.</summary>
    public class ShadeStockProduct
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>The ledger's figure for the product as a whole. This one is authoritative.</summary>
        public decimal SystemStock { get; set; }

        public List<ShadeStockRow> Shades { get; set; } = new();

        /// <summary>
        /// The count these figures are measured from. Null means no count has
        /// ever recorded a shade for this product, so there is nothing to work
        /// forward from and the shade rows have no Left figure.
        /// </summary>
        public DateTime? CountedOn { get; set; }

        /// <summary>
        /// Centimetres used since the count on lines of this product that
        /// record no shade - older lines, and the variants that have no shades
        /// on file. It left the shelf but cannot be charged to any shade, so it
        /// is stated rather than quietly dropped or spread around.
        /// </summary>
        public decimal UnattributedUsed { get; set; }

        /// <summary>
        /// Centimetres received since the count. A delivery is recorded against
        /// the product, never a shade, so it cannot be added to one - it is why
        /// the shade figures can sum to less than the ledger says.
        /// </summary>
        public decimal ReceivedSince { get; set; }

        public bool HasBaseline => CountedOn != null;

        /// <summary>The shade figures added up, where there are any.</summary>
        public decimal? AccountedFor =>
            !HasBaseline ? null : Shades.Sum(s => s.Left ?? 0m);

        /// <summary>
        /// Whether the shade figures and the ledger can be expected to agree.
        /// They cannot once film has gone out with no shade on it, or come in
        /// since the count - so the screen says which, instead of showing a
        /// difference and leaving it to be puzzled over.
        /// </summary>
        public bool Reconciles =>
            HasBaseline && UnattributedUsed == 0m && ReceivedSince == 0m;

        /// <summary>How far the shade figures sit from the ledger.</summary>
        public decimal? Difference =>
            AccountedFor == null ? null : AccountedFor - SystemStock;
    }
}
