using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services.Interfaces
{
    public interface IReportService
    {
        Task<CollectedTotal> GetDailySales();

        Task<CollectedTotal> GetWeeklySales();

        Task<CollectedTotal> GetMonthlySales();

        /// <summary>Products at or below the given stock-on-hand threshold.</summary>
        Task<List<VwStockOnHand>> GetLowStock(decimal threshold = 5);

        Task<List<Sale>> GetRecentSales(int count = 10);
    }
}
