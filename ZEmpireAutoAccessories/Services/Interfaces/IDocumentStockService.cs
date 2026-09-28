using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    /// <summary>
    /// Turns a job order or service invoice into the stock movements
    /// completing it should post. See DocumentStockService.
    /// </summary>
    public interface IDocumentStockService
    {
        Task<IReadOnlyCollection<DocumentStockLine>> ForJobOrder(int jobOrderId);

        Task<IReadOnlyCollection<DocumentStockLine>> ForServiceInvoice(int serviceInvoiceId);

        /// <summary>
        /// Moves this document's stock and keeps a record of what moved, so a
        /// later reversal takes back exactly what was taken.
        /// </summary>
        Task Post(DocumentKind kind, int documentId, bool consume, string userId);
    }
}
