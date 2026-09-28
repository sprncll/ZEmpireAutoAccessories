using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Quotation reads plus thin wrappers over the database stored procedures
    /// dbo.ConvertQuotationToJobOrder and dbo.GetNextInvoiceNumber so the
    /// transactional/number-series logic stays in one place (the database).
    /// </summary>
    public class QuotationService : IQuotationService
    {
        private readonly ApplicationDbContext _context;

        public QuotationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Quotation?> GetQuotation(int quotationId)
        {
            return await _context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Vehicle)
                .Include(q => q.User)
                .Include(q => q.JobType)
                .Include(q => q.JobOrder)
                .Include(q => q.Details)
                    .ThenInclude(d => d.Product!)
                        .ThenInclude(p => p.Category)
                .Include(q => q.Details)
                    .ThenInclude(d => d.Service)
                .Include(q => q.Details)
                    .ThenInclude(d => d.TintVariant)
                .Include(q => q.Details)
                    .ThenInclude(d => d.Panel)
                .FirstOrDefaultAsync(q => q.QuotationID == quotationId);
        }

        public async Task<List<Quotation>> GetQuotations()
        {
            return await _context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Vehicle)
                .Include(q => q.JobOrder)
                .OrderByDescending(q => q.QuotationDate)
                .ToListAsync();
        }

        public async Task<int> ConvertToJobOrder(int quotationId, string userId, string jobOrderNumber)
        {
            var pQuotation = new SqlParameter("@QuotationID", quotationId);
            var pUser = new SqlParameter("@UserId", userId);
            var pNumber = new SqlParameter("@JobOrderNumber", jobOrderNumber);
            var pJobOrderId = new SqlParameter("@JobOrderID", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.ConvertQuotationToJobOrder @QuotationID, @UserId, @JobOrderNumber, @JobOrderID OUTPUT",
                pQuotation, pUser, pNumber, pJobOrderId);

            return (int)pJobOrderId.Value;
        }

        public async Task<(string InvoiceNumber, int InvoiceNoSeriesID)> GetNextInvoiceNumber(string userId)
        {
            var pUser = new SqlParameter("@UserId", userId);
            var pSeriesId = new SqlParameter("@InvoiceNoSeriesID", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            var pInvoiceNumber = new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 50)
            {
                Direction = ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.GetNextInvoiceNumber @UserId, @InvoiceNoSeriesID OUTPUT, @InvoiceNumber OUTPUT",
                pUser, pSeriesId, pInvoiceNumber);

            return ((string)pInvoiceNumber.Value, (int)pSeriesId.Value);
        }
    }
}