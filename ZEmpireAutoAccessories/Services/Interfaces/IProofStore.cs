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
        /// <summary>Whether a proof has been attached to this record.</summary>
        bool Exists(ProofKind kind, int recordId);

        /// <summary>
        /// Which of these records already have a proof. One directory listing
        /// rather than a file probe per row.
        /// </summary>
        HashSet<(ProofKind Kind, int RecordId)> ExistingFor(
            IEnumerable<(ProofKind Kind, int RecordId)> records);

        /// <summary>
        /// Stores <paramref name="file"/> as the proof for this record,
        /// replacing any proof already attached to it. Returns null on
        /// success, or the reason the file was refused.
        /// </summary>
        Task<string?> Save(ProofKind kind, int recordId, IFormFile file);

        /// <summary>
        /// The stored proof, ready to stream, or null if there is none.
        /// </summary>
        (Stream Content, string ContentType, string FileName)? Open(ProofKind kind, int recordId);

        /// <summary>
        /// Removes the proof attached to this record. Returns true if there
        /// was one to remove.
        /// </summary>
        bool Delete(ProofKind kind, int recordId);
    }
}
