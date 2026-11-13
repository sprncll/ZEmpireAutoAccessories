using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    public interface ISalesService
    {
        Task<Sale?> GetSale(int saleId);

        /// <summary>The sales matching the filters, newest first, not yet read.</summary>
        IQueryable<Sale> QuerySales(string? q = null, int? productId = null, DateOnly? dateFrom = null, DateOnly? dateTo = null);

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

        /// <summary>
        /// The panel this line is priced for, where the counter priced it by
        /// panel. Null is a length off the roll, or anything that is not film.
        /// </summary>
        public int? PanelID { get; set; }

        /// <summary>
        /// The class of car that panel was priced for. Needed with the panel
        /// to work out the cut size - what actually leaves the roll.
        /// </summary>
        public int? VehicleClassificationID { get; set; }
    }
}
