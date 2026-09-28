namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// What one document line takes off a roll, for showing on screen.
    ///
    /// A tint line reads "Full Wrap x1" and used to carry the unit "pc",
    /// which says nothing true: the roll does not lose a piece of anything,
    /// it loses the length that panel takes on that class of vehicle. This
    /// carries what the line needs to say so instead.
    /// </summary>
    public class PanelFilm
    {
        /// <summary>Whether the product on the line comes off a roll.</summary>
        public bool SoldByLength { get; set; }

        public int? PanelID { get; set; }
        public int? VehicleClassificationID { get; set; }

        /// <summary>How many of that panel the line covers.</summary>
        public decimal Quantity { get; set; } = 1m;

        /// <summary>The unit stored on the line, for anything not measured.</summary>
        public string? StoredUnit { get; set; }

        /// <summary>
        /// Whether this line's stock comes from a cut size rather than from
        /// the quantity as written.
        /// </summary>
        public bool IsMeasured => SoldByLength && PanelID != null;
    }
}
