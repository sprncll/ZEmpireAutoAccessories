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

        /// <summary>
        /// The unit the grid is shown and typed in. Held in centimetres
        /// underneath either way - this only decides what a box means.
        /// </summary>
        public string Unit { get; set; } = UnitOfMeasure.Meter;

        /// <summary>Centimetres already on record, by classification and panel.</summary>
        public Dictionary<(int VehicleClassificationID, int PanelID), decimal> Centimeters { get; set; } = new();

        /// <summary>What is on record for this pairing, in the chosen unit.</summary>
        public decimal? For(int classificationId, int panelId) =>
            Centimeters.TryGetValue((classificationId, panelId), out var cm)
                ? UnitOfMeasure.FromBase(cm, Unit)
                : null;

        /// <summary>
        /// What this pairing would be filled with, in the chosen unit, where
        /// nothing is on record yet and there is a suggestion for it.
        /// </summary>
        public decimal? Suggested(VehicleClassification classification, Panel panel)
        {
            if (Centimeters.ContainsKey((classification.VehicleClassificationID, panel.PanelID)))
                return null;

            var cm = SuggestedCutSizes.Centimeters(classification.ClassificationName, panel.PanelName);
            return cm == null ? null : UnitOfMeasure.FromBase(cm.Value, Unit);
        }

        /// <summary>How many blank boxes a fill would put a number in.</summary>
        public int Fillable =>
            Classifications.Sum(c => Panels.Count(p => Suggested(c, p) != null));

        /// <summary>How a figure is written in the chosen unit.</summary>
        public string Format(decimal value) =>
            value.ToString(Unit == UnitOfMeasure.Centimeter ? "0.#" : "0.##",
                           System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>How many boxes are filled in, out of how many there are.</summary>
        public int Filled => Centimeters.Count;
        public int Total => Classifications.Count * Panels.Count;
    }
}
