using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// How much film one panel takes on one class of vehicle. See
    /// CutSizeStore for why this is a file rather than a table.
    /// </summary>
    public interface ICutSizeStore
    {
        /// <summary>
        /// Centimetres of film for this panel on this class of vehicle, or
        /// null if nobody has set one. Null means "not known", never zero -
        /// a panel that took no film would not be on the job.
        /// </summary>
        decimal? Centimeters(int vehicleClassificationId, int panelId);

        /// <summary>Every cut size on record.</summary>
        IReadOnlyCollection<CutSize> All();

        /// <summary>
        /// Replaces the whole table. Entries at or below zero are dropped, so
        /// clearing a box on the form removes that cut size rather than
        /// recording that a panel takes nothing.
        /// </summary>
        Task Save(IEnumerable<CutSize> sizes);
    }
}
