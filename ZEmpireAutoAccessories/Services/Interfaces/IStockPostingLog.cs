using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// What a document actually took off the shelf, so putting it back takes
    /// back the same. See StockPostingLog.
    /// </summary>
    public interface IStockPostingLog
    {
        /// <summary>What this document took, or null if nothing is on record.</summary>
        IReadOnlyCollection<DocumentStockLine>? Taken(DocumentKind kind, int documentId);

        Task Record(DocumentKind kind, int documentId, IEnumerable<DocumentStockLine> lines);

        Task Clear(DocumentKind kind, int documentId);
    }
}
