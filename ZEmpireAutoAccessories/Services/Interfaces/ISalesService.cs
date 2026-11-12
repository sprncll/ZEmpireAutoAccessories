using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    public interface ISalesService
    {
        Task<Sale?> GetSale(int saleId);

        Task<List<Sale>> GetSales(string? q = null, int? productId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null);

        Task<int> GetUnitsSold(int productId, DateOnly? dateFrom = null, DateOnly? dateTo = null);

        Task<Sale> CreateSale(
            string userId,
            int customerId,
            int paymentModeId,
            string invoiceNumber,
            int? vehicleId,
            List<SaleLineRequest> items);
    }

    public class SaleLineRequest
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Which shade of the film is being sold, where the line is film.
        /// Null for everything else, and for a film whose shades nobody has
        /// put on file.
        /// </summary>
        public int? ShadeID { get; set; }
    }
}
