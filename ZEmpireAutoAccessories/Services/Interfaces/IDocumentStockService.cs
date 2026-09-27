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
    }
}
