using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Authorization;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Controllers
{
    [ModuleAuthorize("Sales")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISalesService _salesService;
        private readonly IQuotationService _quotationService; // GetNextInvoiceNumber lives here
        private readonly IInventoryService _inventoryService;
        private readonly IShadeStockService _shadeStock;

        public SalesController(
            ApplicationDbContext context,
            ISalesService salesService,
            IQuotationService quotationService,
            IInventoryService inventoryService,
            IShadeStockService shadeStock)
        {
            _context = context;
            _salesService = salesService;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
            _shadeStock = shadeStock;
        }

        // GET: Sale?q=...&productId=...&dateFrom=...&dateTo=...
        public async Task<IActionResult> Index(string? q, int? productId, DateOnly? dateFrom, DateOnly? dateTo)
        {
            var sales = await _salesService.GetSales(q, productId, dateFrom, dateTo);

            ViewData["Search"] = q;
            ViewData["ProductID"] = new SelectList(
                await _context.Products.OrderBy(p => p.ProductName).ToListAsync(),
                "ProductID", "ProductName", productId);
            ViewData["DateFrom"] = dateFrom;
            ViewData["DateTo"] = dateTo;

            if (productId.HasValue)
                ViewData["UnitsSold"] = await _salesService.GetUnitsSold(productId.Value, dateFrom, dateTo);

            return View(sales);
        }

        // GET: Sale/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _salesService.GetSale(id.Value);
            if (sale == null)
                return NotFound();

            ViewData["WarrantyByLine"] = await LoadWarrantiesForLines(
                sale.SaleDetails.Select(d => d.SalesDetailID).ToList());

            return View(sale);
        }

        /// <summary>
        /// The warranty already covering each sale line, keyed by SalesDetailID,
        /// so the details screen can offer "+ Warranty" on lines without one and
        /// link to the existing record on lines that have one. sales.Warranty
        /// does not stop a line being covered twice, so the newest wins.
        /// </summary>
        private async Task<Dictionary<int, Warranty>> LoadWarrantiesForLines(List<int> salesDetailIds)
        {
            if (salesDetailIds.Count == 0)
                return new Dictionary<int, Warranty>();

            var warranties = await _context.Warranties
                .Where(w => w.SalesDetailID != null && salesDetailIds.Contains(w.SalesDetailID.Value))
                .OrderByDescending(w => w.WarrantyID)
                .ToListAsync();

            return warranties
                .GroupBy(w => w.SalesDetailID!.Value)
                .ToDictionary(g => g.Key, g => g.First());
        }

        // GET: Sale/Pdf/5
        public async Task<IActionResult> Pdf(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _salesService.GetSale(id.Value);
            if (sale == null)
                return NotFound();

            var pdf = DocumentPdfBuilder.BuildSalePdf(sale);
            return File(pdf, "application/pdf", $"{sale.InvoiceNumber}.pdf");
        }

        // POST: Sale/QuickCustomer
        //
        // A walk-in buying a bottle of wax is still a customer, and sending
        // the person at the counter off to the Customers module to make one
        // loses the sale they had half-typed. This adds the two fields a sale
        // actually needs and hands the id straight back to the form.
        //
        // It only ever ADDS. A number already on file is refused rather than
        // quietly resolved to whoever holds it: picking a customer the typist
        // did not choose is how a sale ends up against the wrong person.
        //
        // It lives here rather than on CustomerController because that one is
        // behind the Customers module, and somebody who may ring up a sale is
        // not necessarily given it. Making a customer is part of making a
        // sale, so the Sales module's own permission is the right one to ask
        // for.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCustomer(
            string? lastName, string? firstName, string? contactNumber)
        {
            // Two boxes, one column - the same split the Customers module
            // uses, so a walk-in added here is not a different shape of
            // record from one added there.
            var fullName = PersonName.Combine(firstName, lastName);

            if (string.IsNullOrWhiteSpace(firstName))
                return Json(new { error = "Enter the customer's first name." });

            if (string.IsNullOrWhiteSpace(lastName))
                return Json(new { error = "Enter the customer's last name." });

            var number = PhoneNumber.ToPhilippineMobile(contactNumber);
            if (number == null)
                return Json(new { error = "Enter a Philippine mobile number: +63 then 10 digits starting with 9." });

            // This panel adds a customer; it does not find one. A number
            // already on file is refused and the holder named, the same way
            // the Customers module refuses it, so the two screens cannot
            // disagree about whether a number may be reused. Naming them is
            // the point: the customer is already in the dropdown behind this
            // panel, and whoever is typing needs to know who to pick.
            //
            // Both shapes are looked for: a row entered before the shop moved
            // to +63 still holds 09121231231, which is the same number.
            var dialled = PhoneNumber.ToDialledForm(number);

            var holder = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContactNumber == number || c.ContactNumber == dialled);

            if (holder != null)
                return Json(new
                {
                    error = $"That number is already on file for \"{holder.FullName}\". "
                            + "Close this and pick them from the Customer list."
                });

            var customer = new Customer
            {
                FullName = fullName,
                ContactNumber = number,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return Json(new { id = customer.CustomerID, name = customer.FullName });
        }

        // GET: Sale/VehiclesForCustomer?customerId=5
        public async Task<IActionResult> VehiclesForCustomer(int customerId)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.CustomerID == customerId)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    value = v.VehicleID,
                    text = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model
                })
                .ToListAsync();

            return Json(vehicles);
        }

        // GET: Sale/PricingForVehicle?vehicleId=5
        // Every Pricing row for this vehicle's classification, so the line-item
        // rows can look up the real matrix price (Product x Tint Variant x
        // Panel) client-side instead of just the product's flat DefaultPrice.
        public async Task<IActionResult> PricingForVehicle(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
                return Json(new List<object>());

            var matrix = await _context.Pricings
                .Where(p => p.VehicleClassificationID == vehicle.VehicleClassificationID)
                .Select(p => new
                {
                    productId = p.ProductID,
                    tintVariantId = p.TintVariantID,
                    tintVariantName = p.TintVariant != null ? p.TintVariant.VariantName : null,
                    panelId = p.PanelID,
                    panelName = p.Panel.PanelName,
                    price = p.Price
                })
                .ToListAsync();

            return Json(matrix);
        }

        // GET: Sale/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View();
        }

        // POST: Sale/Create
        // Bound as parallel flat lists (not List<SaleLineRequest>) since the Create
        // view builds rows client-side with plain repeated input names - simpler
        // than indexed complex-object binding for a small line-item table.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int customerId,
            int? vehicleId,
            int paymentModeId,
            List<int> productId,
            List<int> quantity,
            List<decimal> unitPrice,
            decimal? amountTendered = null)
        {
            productId ??= new List<int>();
            quantity ??= new List<int>();
            unitPrice ??= new List<decimal>();

            // The three lists are one table read column by column, and they
            // only line up while every box on every row posts a value. An
            // empty Unit Price does not: the binder cannot make a decimal of
            // "", so that row's price is simply missing and every price after
            // it belongs to the row above. Reading them by index then walked
            // off the end of the list - an unhandled exception at the counter,
            // with the customer waiting, instead of a word about the one box
            // that was blank.
            //
            // A price is blank when a panel-priced film has no panel picked
            // yet, or the price list has no row for the one that is - and the
            // Price box is read-only to staff, so nobody could type over it
            // either. Say which line it is and send the form back.
            if (quantity.Count != productId.Count || unitPrice.Count != productId.Count)
            {
                ModelState.AddModelError(string.Empty,
                    "A line is missing its price. For a product priced by panel, pick the "
                    + "Tint Variant and Panel so the price can be looked up - if there is no "
                    + "price for that combination yet, an administrator sets one under Pricing.");

                await LoadDropdowns();
                return View();
            }

            var items = new List<SaleLineRequest>();
            for (int i = 0; i < productId.Count; i++)
            {
                if (productId[i] > 0 && quantity[i] > 0)
                {
                    items.Add(new SaleLineRequest
                    {
                        ProductID = productId[i],
                        Quantity = quantity[i],
                        UnitPrice = unitPrice[i]
                    });
                }
            }

            // The same PRODUCT on two rows is one line of two, not two
            // lines of one - the rule the job order and the quotation keep,
            // and a receipt a customer reads should add up the same way.
            // Four rows of "Armor Rear Side Step" at 1 tell nobody anything
            // the single row "4" does not, and they invite somebody voiding
            // a duplicate to take off a quantity that was really sold.
            //
            // Only lines that agree on the price are folded: a different
            // price is a different deal and stays its own line. The first
            // appearance keeps its place in the list, so the receipt reads in
            // the order the counter wrote it.
            items = items
                .GroupBy(i => new { i.ProductID, i.UnitPrice })
                .Select(g => new SaleLineRequest
                {
                    ProductID = g.Key.ProductID,
                    UnitPrice = g.Key.UnitPrice,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Add at least one line item.");
                await LoadDropdowns();
                return View();
            }

            // Staff sell at the price in the book; administrators set what
            // the book says. The counter form fills the price in and shows it
            // read-only, but a form is only a suggestion, so each line is
            // checked here against what the book actually holds for that
            // product: its own price, or any panel price for the class of car
            // on the sale. Anything else is refused rather than quietly
            // corrected - at a counter, with the customer standing there, a
            // price that silently changes between typing and printing is
            // worse than being told no.
            if (!AppRoles.CanEditCatalogPrices(User))
            {
                var vehicleClassificationId = vehicleId is { } vid
                    ? await _context.Vehicles
                        .Where(v => v.VehicleID == vid)
                        .Select(v => (int?)v.VehicleClassificationID)
                        .FirstOrDefaultAsync()
                    : null;

                foreach (var line in items)
                {
                    var product = await _context.Products
                        .Where(x => x.ProductID == line.ProductID)
                        .Select(x => new { x.ProductName, x.DefaultPrice })
                        .FirstOrDefaultAsync();

                    var inTheBook =
                        product?.DefaultPrice is { } flat && flat == line.UnitPrice;

                    if (!inTheBook && vehicleClassificationId != null)
                    {
                        inTheBook = await _context.Pricings.AnyAsync(pr =>
                            pr.ProductID == line.ProductID &&
                            pr.VehicleClassificationID == vehicleClassificationId &&
                            pr.Price == line.UnitPrice);
                    }

                    // A product the book has no price for at all is the
                    // one case a staff member may still name one: refusing it
                    // would mean a sale nobody can ring up until an
                    // administrator is found, with the customer at the
                    // counter. The same carve-out the job order has.
                    if (!inTheBook)
                    {
                        var hasAnyPrice =
                            product?.DefaultPrice is { } any && any > 0;

                        if (!hasAnyPrice && vehicleClassificationId != null)
                        {
                            hasAnyPrice = await _context.Pricings.AnyAsync(pr =>
                                pr.ProductID == line.ProductID &&
                                pr.VehicleClassificationID == vehicleClassificationId);
                        }

                        inTheBook = !hasAnyPrice;
                    }

                    if (!inTheBook)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"{product?.ProductName ?? "That product"} is not sold at "
                            + $"{Money.Peso(line.UnitPrice)}. Prices come from the price list - "
                            + "pick the product again to pull its price, or ask an administrator "
                            + "to change what the list says.");

                        await LoadDropdowns();
                        return View();
                    }
                }
            }

            try
            {
                var (invoiceNumber, _) = await _quotationService.GetNextInvoiceNumber(CurrentUserId);
                var sale = await _salesService.CreateSale(
                    CurrentUserId, customerId, paymentModeId, invoiceNumber, vehicleId, items);

                // Details opens the receipt by itself on this one visit.
                TempData["SaleCompleted"] = sale.InvoiceNumber;

                // sales.Sales holds the total and nothing else - no tender, no
                // change, no status - so this is carried to the receipt page
                // for this one visit rather than stored. It is what the cashier
                // needs while the customer is still at the counter; the sale
                // record itself is unchanged. A figure that does not cover the
                // total is dropped rather than reported as change: the form
                // will not submit one, so it can only be a stale or tampered
                // post, and a wrong change figure is worse than none.
                //
                // An online mode sends exactly what it was asked for, so the
                // figure is the total whatever arrived on the form.
                var paymentMode = await _context.PaymentModes
                    .Where(p => p.PaymentModeID == paymentModeId)
                    .Select(p => p.PaymentModeName)
                    .FirstOrDefaultAsync();

                if (PaymentModes.SettlesExactly(paymentMode))
                    amountTendered = sale.TotalAmount;

                // Written as text because TempData's serializer refuses a
                // decimal outright - it would throw at the end of this request
                // and take the whole sale down with it.
                if (amountTendered is { } tendered && tendered >= sale.TotalAmount)
                {
                    // Grouped the way every other figure on the screen is, so
                    // the amount handed over reads like the total beside it.
                    TempData["SaleTendered"] = Money.Plain(tendered);
                    TempData["SaleChange"] = Money.Plain(tendered - sale.TotalAmount);
                }

                return RedirectToAction(nameof(Details), new { id = sale.SalesID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadDropdowns();
                return View();
            }
        }

        // GET: Sale/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.PaymentMode)
                .FirstOrDefaultAsync(s => s.SalesID == id);

            if (sale == null)
                return NotFound();

            ViewData["BlockReason"] = await BuildBlockReason(id.Value);

            return View(sale);
        }

        // POST: Sale/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale == null)
                return RedirectToAction(nameof(Index));

            var blockReason = await BuildBlockReason(id);
            if (blockReason != null)
            {
                TempData["DeleteError"] = blockReason;
                return RedirectToAction(nameof(Delete), new { id });
            }

            try
            {
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["DeleteError"] =
                    "Can't delete this sale. It still has related records elsewhere in the system.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            return RedirectToAction(nameof(Index));
        }

        // Sale -> SaleDetail cascades, but a Warranty referencing one of those
        // lines is ON DELETE RESTRICT, which blocks the cascade. Note: deleting a
        // sale does NOT reverse the inventory OUT transactions it posted.
        private async Task<string?> BuildBlockReason(int saleId)
        {
            var warranties = await _context.Warranties
                .CountAsync(w => w.SalesDetail != null && w.SalesDetail.SalesID == saleId);

            if (warranties == 0)
                return null;

            return $"Can't delete this sale. {warranties} warranty record{(warranties == 1 ? "" : "s")} " +
                   "still reference its line items. Remove those first.";
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task LoadDropdowns()
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                "CustomerID", "FullName");

            var vehicles = await _context.Vehicles
                .Include(v => v.Customer)
                .OrderBy(v => v.PlateNumber)
                .Select(v => new
                {
                    v.VehicleID,
                    Display = (v.PlateNumber ?? "No Plate") + " - " + v.Brand + " " + v.Model + " (" + v.Customer.FullName + ")"
                })
                .ToListAsync();
            ViewData["VehicleID"] = new SelectList(vehicles, "VehicleID", "Display");

            // Only the modes the shop offers, in the order it offers them -
            // see Models/PaymentModes.cs.
            var paymentModes = PaymentModes.ForSelection(await _context.PaymentModes.ToListAsync());

            ViewData["PaymentModeID"] = new SelectList(
                paymentModes, "PaymentModeID", "PaymentModeName");

            // Which of them are charged the exact total - everything except
            // cash. The form fills the tendered amount in and locks it for
            // those; there is no bigger note and no change to give back.
            ViewData["ExactPaymentModeIds"] = paymentModes
                .Where(m => PaymentModes.SettlesExactly(m.PaymentModeName))
                .Select(m => m.PaymentModeID)
                .ToList();

            var saleProducts = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
            ViewData["Products"] = saleProducts;

            // Film and PPF are sold off the roll by the metre, so their lines
            // are counted in metres and their stock reads in metres too.
            ViewData["RollProducts"] =
                await _inventoryService.SoldByLength(saleProducts.Select(p => p.ProductID));

            // Stock on hand per product, so a line can warn while it is being
            // typed instead of the whole sale failing in CreateSale. Same
            // arithmetic as dbo.vw_StockOnHand and the check that service
            // makes - this is the friendly half, not the authority.
            ViewData["StockByProduct"] = await _context.InventoryTransactions
                .GroupBy(t => t.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Stock = g.Sum(t => t.TransactionType == "IN" ? t.Quantity : -t.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Stock);

            // What is on the rack, shade by shade, for the films that have
            // shades on file.
            //
            // A customer buying tint asks which shades there are before they
            // ask anything else, and the figure on the product line - "92.61 m
            // in stock" - cannot answer it: inv.InventoryTransaction records a
            // product and nothing finer. ShadeStock works the breakdown out
            // from the last count and the lines since, which is an estimate
            // and says so.
            //
            // Shown, not recorded: sales.SalesDetail has no shade column, so a
            // counter sale of film still comes off the product as a whole.
            // Telling the customer what is there is worth doing anyway, and is
            // the half that needs no table to change.
            ViewData["ShadeStock"] = (await _shadeStock.Build())
                .Select(p => new
                {
                    productId = p.ProductID,
                    counted = p.Shades.Any(sh => sh.Left != null),
                    shades = p.Shades
                        .OrderByDescending(sh => sh.Left ?? -1m)
                        .ThenBy(sh => sh.Label)
                        .Select(sh => new
                        {
                            label = sh.Label,
                            left = sh.Left,
                            exhausted = sh.Exhausted
                        })
                        .ToList()
                })
                .ToList();
        }
    }
}
