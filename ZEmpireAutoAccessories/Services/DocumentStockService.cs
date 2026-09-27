using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// What completing a document should take off the shelf.
    ///
    /// For anything counted this is the line as written: three floor mats are
    /// three floor mats. Film is different. Tinting is sold by the panel, so
    /// the line reads "Front Windshield x1" and the price comes from
    /// cat.Pricing - but the roll does not lose one of anything, it loses the
    /// length that panel takes on that class of vehicle. Deducting the line's
    /// quantity would take a centimetre off a roll for a job that used well
    /// over a metre, and the shelf figure would drift away from the shelf
    /// within a week.
    ///
    /// So a film line that names a panel is measured from the cut sizes
    /// (ICutSizeStore) and posted in centimetres. A film line with no panel -
    /// PPF sold by the metre, an offcut - keeps the length that was typed.
    ///
    /// A panel with no cut size on record stops the document rather than
    /// guessing. Guessing here is a wrong number on the shelf that nobody
    /// notices until a stock count; stopping is a message naming the panel
    /// and the vehicle class to go and set.
    /// </summary>
    public class DocumentStockService : IDocumentStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventory;
        private readonly ICutSizeStore _cutSizes;

        public DocumentStockService(
            ApplicationDbContext context,
            IInventoryService inventory,
            ICutSizeStore cutSizes)
        {
            _context = context;
            _inventory = inventory;
            _cutSizes = cutSizes;
        }

        public async Task<IReadOnlyCollection<DocumentStockLine>> ForJobOrder(int jobOrderId)
        {
            var classification = await _context.JobOrders
                .Where(j => j.JobOrderID == jobOrderId)
                .Select(j => (int?)j.Vehicle.VehicleClassificationID)
                .FirstOrDefaultAsync();

            var rows = await _context.JobOrderDetails
                .Where(d => d.JobOrderID == jobOrderId && d.ProductID != null)
                .Select(d => new LineRow
                {
                    ProductID = d.ProductID!.Value,
                    ProductName = d.Product!.ProductName,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    PanelID = d.PanelID,
                    PanelName = d.Panel!.PanelName
                })
                .ToListAsync();

            return await Build(rows, classification);
        }

        public async Task<IReadOnlyCollection<DocumentStockLine>> ForServiceInvoice(int serviceInvoiceId)
        {
            // Vehicle is optional on an invoice, so this can come back null -
            // which only matters if a panel line turns up, and Build says so.
            var classification = await _context.ServiceInvoices
                .Where(i => i.ServiceInvoiceID == serviceInvoiceId)
                .Select(i => i.Vehicle != null ? (int?)i.Vehicle.VehicleClassificationID : null)
                .FirstOrDefaultAsync();

            var rows = await _context.ServiceInvoiceDetails
                .Where(d => d.ServiceInvoiceID == serviceInvoiceId && d.ProductID != null)
                .Select(d => new LineRow
                {
                    ProductID = d.ProductID!.Value,
                    ProductName = d.Product!.ProductName,
                    Quantity = d.Quantity,
                    Unit = d.Unit,
                    PanelID = d.PanelID,
                    PanelName = d.Panel!.PanelName
                })
                .ToListAsync();

            return await Build(rows, classification);
        }

        private async Task<IReadOnlyCollection<DocumentStockLine>> Build(
            List<LineRow> rows, int? vehicleClassificationId)
        {
            if (rows.Count == 0)
                return Array.Empty<DocumentStockLine>();

            var byLength = await _inventory.SoldByLength(rows.Select(r => r.ProductID));

            var classificationName = vehicleClassificationId is { } id
                ? await _context.VehicleClassifications
                    .Where(c => c.VehicleClassificationID == id)
                    .Select(c => c.ClassificationName)
                    .FirstOrDefaultAsync()
                : null;

            var lines = new List<DocumentStockLine>(rows.Count);

            foreach (var row in rows)
            {
                // Counted goods, and film with no panel against it, are the
                // quantity as written - the line's own unit says what in.
                if (!byLength.Contains(row.ProductID) || row.PanelID == null)
                {
                    lines.Add(new DocumentStockLine(row.ProductID, row.Quantity, row.Unit));
                    continue;
                }

                if (vehicleClassificationId == null)
                    throw new InvalidOperationException(
                        $"{row.ProductName} is cut to fit {row.PanelName}, but this document has no " +
                        "vehicle on it, so there is no classification to measure against. " +
                        "Set the vehicle first.");

                var centimeters = _cutSizes.Centimeters(vehicleClassificationId.Value, row.PanelID.Value);

                if (centimeters == null)
                    throw new CutSizeMissingException(
                        $"No cut size is set for {row.PanelName} on " +
                        $"{classificationName ?? "this class of vehicle"}. " +
                        "Set it under Inventory → Cut Sizes, then complete this document again.");

                // One line can cover the same panel more than once - two rear
                // door glasses written as a quantity of two.
                lines.Add(new DocumentStockLine(
                    row.ProductID,
                    centimeters.Value * row.Quantity,
                    UnitOfMeasure.Centimeter));
            }

            return lines;
        }

        /// <summary>
        /// What Build needs from a line, flattened so a job order detail and a
        /// service invoice detail - different tables, same shape here - go
        /// through one path.
        /// </summary>
        private sealed class LineRow
        {
            public int ProductID { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public decimal Quantity { get; init; }
            public string? Unit { get; init; }
            public int? PanelID { get; init; }
            public string? PanelName { get; init; }
        }
    }
}
