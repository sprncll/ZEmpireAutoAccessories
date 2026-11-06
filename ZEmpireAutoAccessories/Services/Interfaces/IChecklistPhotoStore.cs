using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// The photographs taken at intake, filed against the checklist they
    /// belong to. See ChecklistPhotoStore for why they are a folder rather
    /// than a table.
    /// </summary>
    public interface IChecklistPhotoStore
    {
        /// <summary>How many photos one checklist may hold.</summary>
        int MaxPerChecklist { get; }

        /// <summary>The largest single photo this store will take.</summary>
        long MaxBytes { get; }

        /// <summary>
        /// The photos on this checklist, oldest first. Empty when there are
        /// none, which is the normal case for a checklist nobody photographed.
        /// </summary>
        IReadOnlyList<ChecklistPhoto> List(int checklistId);

        /// <summary>
        /// How many photos each of these checklists holds. One directory
        /// listing rather than a probe per row, for the list screen.
        /// </summary>
        Dictionary<int, int> CountsFor(IEnumerable<int> checklistIds);

        /// <summary>
        /// Files <paramref name="file"/> against this checklist. Returns null
        /// on success, or the reason it was refused.
        /// </summary>
        Task<string?> Save(int checklistId, IFormFile file);

        /// <summary>
        /// One stored photo, ready to stream, or null if that slot is empty.
        /// </summary>
        (Stream Content, string ContentType, string FileName)? Open(int checklistId, int slot);

        /// <summary>
        /// Removes one photo. Returns true if there was one in that slot.
        /// </summary>
        bool Remove(int checklistId, int slot);

        /// <summary>
        /// Removes every photo on a checklist, for when the checklist itself
        /// is deleted and the folder would otherwise keep them for good.
        /// </summary>
        void RemoveAll(int checklistId);
    }
}
