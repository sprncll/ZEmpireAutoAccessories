using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// Where a file kept as evidence lives - proof that an online payment
    /// arrived, or that a warranty was claimed. See ProofStore for why it is a
    /// folder rather than a table.
    /// </summary>
    public interface IProofStore
    {
        /// <summary>The most files one record may hold.</summary>
        int MaxPerRecord { get; }

        /// <summary>Whether anything has been attached to this record.</summary>
        bool Exists(ProofKind kind, int recordId);

        /// <summary>
        /// The numbers of the files attached to this record, in the order they
        /// were added. Empty when there are none.
        /// </summary>
        IReadOnlyList<int> Slots(ProofKind kind, int recordId);

        /// <summary>
        /// The same, for many records at once. One directory listing rather
        /// than a file probe per row; a record with nothing attached has no
        /// entry.
        /// </summary>
        Dictionary<(ProofKind Kind, int RecordId), List<int>> SlotsFor(
            IEnumerable<(ProofKind Kind, int RecordId)> records);

        /// <summary>
        /// Stores <paramref name="file"/> as the only proof for this record,
        /// replacing whatever was attached to it. Returns null on success, or
        /// the reason the file was refused.
        /// </summary>
        Task<string?> Save(ProofKind kind, int recordId, IFormFile file);

        /// <summary>
        /// Stores <paramref name="file"/> alongside what this record already
        /// has - a second receipt for a second payment. Returns null on
        /// success, or the reason the file was refused.
        /// </summary>
        Task<string?> Add(ProofKind kind, int recordId, IFormFile file);

        /// <summary>
        /// One stored proof, ready to stream, or null if there is none with
        /// that number.
        /// </summary>
        (Stream Content, string ContentType, string FileName)? Open(
            ProofKind kind, int recordId, int slot = 1);

        /// <summary>
        /// Removes one file from this record, or all of them when no number
        /// is given. Returns true if there was anything to remove.
        /// </summary>
        bool Delete(ProofKind kind, int recordId, int? slot = null);
    }
}
