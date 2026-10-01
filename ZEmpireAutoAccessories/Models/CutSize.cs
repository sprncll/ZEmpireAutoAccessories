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
        /// The pairings a film product is actually priced for, from
        /// cat.Pricing. The add-line form builds its panel list from those
        /// rows, so a pairing with no price cannot be put on a document and
        /// its cut size would never be read.
        /// </summary>
        public HashSet<(int VehicleClassificationID, int PanelID)> Sold { get; set; } = new();

        public bool IsSold(int classificationId, int panelId) =>
            Sold.Contains((classificationId, panelId));

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
        ///
        /// Only for pairings that are sold. Filling in a cut size for film
        /// nobody prices for that class of vehicle is work with no effect -
        /// the panel never appears on a document, so the figure is never read.
        /// </summary>
        public decimal? Suggested(VehicleClassification classification, Panel panel)
        {
            if (!IsSold(classification.VehicleClassificationID, panel.PanelID))
                return null;

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

        /// <summary>
        /// How many of the pairings that are actually sold have a cut size,
        /// out of how many there are. Counted against what is sellable rather
        /// than every square in the grid - most of those cross a panel with a
        /// class that never has it.
        /// </summary>
        public int Filled => Sold.Count(s => Centimeters.ContainsKey(s));
        public int Total => Sold.Count;

        /// <summary>Sold pairings with no cut size - the ones that will stop a job.</summary>
        public int Missing => Total - Filled;
    }
}
