namespace ZEmpireAutoAccessories.Models
{
    /// <summary>
    /// How much film one panel takes on one class of vehicle - a front
    /// windshield on a sedan is not a front windshield on a van.
    ///
    /// Held in centimetres, the unit stock itself is held in, so deducting is
    /// a subtraction and not a conversion. The screens read and write metres,
    /// which is how a roll is bought and how anyone at the shop talks about
    /// it.
    /// </summary>
    public class CutSize
    {
        public int VehicleClassificationID { get; set; }
        public int PanelID { get; set; }

        /// <summary>Centimetres of film. Always more than zero.</summary>
        public decimal Centimeters { get; set; }

        /// <summary>The same length in metres, for the screens.</summary>
        public decimal Meters => UnitOfMeasure.FromBase(Centimeters, UnitOfMeasure.Meter);
    }

    /// <summary>
    /// The cut-size screen: classifications down the side, panels across the
    /// top, and whatever has been set so far.
    /// </summary>
    public class CutSizeGrid
    {
        public List<VehicleClassification> Classifications { get; set; } = new();
        public List<Panel> Panels { get; set; } = new();

        /// <summary>Metres already on record, by classification and panel.</summary>
        public Dictionary<(int VehicleClassificationID, int PanelID), decimal> Meters { get; set; } = new();

        public decimal? For(int classificationId, int panelId) =>
            Meters.TryGetValue((classificationId, panelId), out var m) ? m : null;

        /// <summary>How many boxes are filled in, out of how many there are.</summary>
        public int Filled => Meters.Count;
        public int Total => Classifications.Count * Panels.Count;
    }
}
