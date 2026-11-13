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
        private readonly ICutSizeStore _cutSizes;

        public SalesController(
            ApplicationDbContext context,
            ISalesService salesService,
            IQuotationService quotationService,
            IInventoryService inventoryService,
            IShadeStockService shadeStock,
            ICutSizeStore cutSizes)
        {
            _context = context;
            _salesService = salesService;
            _quotationService = quotationService;
            _inventoryService = inventoryService;
            _shadeStock = shadeStock;
            _cutSizes = cutSizes;
        }

        // GET: Sale?q=...&productId=...&dateFrom=...&dateTo=...
        public async Task<IActionResult> Index(string? q, int? productId, DateOnly? dateFrom, DateOnly? dateTo, int? page = null)
        {
            var sales = await _salesService.QuerySales(q, productId, dateFrom, dateTo).ToPageAsync(page, ViewData);

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
        // loses the sale they had half-typed. The rules are in
        // Services/QuickCustomer, shared with the quotation screen, which
        // asks the same question for the same reason.
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
            var result = await Services.QuickCustomer.Add(
                _context, lastName, firstName, contactNumber);

            return result.Error != null
                ? Json(new { error = result.Error })
                : Json(new { id = result.Id, name = result.Name });
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

        // GET: Sale/PricingForClassification?classificationId=3
        //
        // The price list for one class of car, for a line that names its own
        // class rather than taking it from a vehicle on the sale. A walk-in
        // asking for tint has no vehicle on file, and the price list is keyed
        // by the class of car, so the counter has to be able to say "sedan"
        // and get the sedan prices.
        //
        // The cut size rides along with each row: it is what actually leaves
        // the roll, so the counter can see it before the sale is taken and
        // the quantity box does not have to lie about metres.
        public IActionResult PricingForClassification(int classificationId) =>
            Json(new
            {
                classificationId,
                rows = PriceRows(classificationId)
            });

        private List<object> PriceRows(int classificationId) =>
            _context.Pricings
                .Where(p => p.VehicleClassificationID == classificationId)
                .Select(p => new
                {
                    productId = p.ProductID,
                    tintVariantId = p.TintVariantID,
                    tintVariantName = p.TintVariant != null ? p.TintVariant.VariantName : null,
                    panelId = p.PanelID,
                    panelName = p.Panel.PanelName,
                    price = p.Price
                })
                .AsEnumerable()
                .Select(p => (object)new
                {
                    p.productId,
                    p.tintVariantId,
                    p.tintVariantName,
                    p.panelId,
                    p.panelName,
                    p.price,
                    cutCm = _cutSizes.Centimeters(classificationId, p.panelId)
                })
                .ToList();

        // GET: Sale/PricingForVehicle?vehicleId=5
        // Every Pricing row for this vehicle's classification, so the line-item
        // rows can look up the real matrix price (Product x Tint Variant x
        // Panel) client-side instead of just the product's flat DefaultPrice.
        public async Task<IActionResult> PricingForVehicle(int vehicleId)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
                return Json(new List<object>());

            // The same shape PricingForClassification returns, and the
            // class itself with it: picking the customer's car is also a way
            // of saying which class every film line on this sale is for.
            return Json(new
            {
                classificationId = vehicle.VehicleClassificationID,
                rows = PriceRows(vehicle.VehicleClassificationID)
            });
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
            decimal? amountTendered = null,

            // Posted as strings rather than as int?, because this column has
            // to stay the same length as the three beside it: a blank in a
            // List<int?> is dropped by the binder, every shade after it slides
            // up a row, and the lines quietly belong to the wrong film.
            List<string>? shadeId = null,

            // Posted as strings for the same reason the shade is: a blank in
            // a List<int?> is dropped by the binder and every value after it
            // slides up a row.
            List<string>? panelId = null,
            List<string>? classificationId = null)
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

            // Which shade of film each line is taking off the roll, where
            // the line is film. A shade belongs to exactly one tint variant
            // and a variant to one product, so a shade that does not belong
            // to the product on its row is not a shade for that line - it is
            // dropped rather than stored, since a wrong shade is subtracted
            // from a real rack.
            var postedShades = (shadeId ?? new List<string>())
                .Select(v => int.TryParse(v, out var parsed) ? parsed : (int?)null)
                .ToList();

            var shadeOwners = postedShades.Any(v => v != null)
                ? await _context.Shades
                    .Where(sh => postedShades.Contains(sh.ShadeID))
                    .Select(sh => new { sh.ShadeID, sh.TintVariant.ProductID })
                    .ToDictionaryAsync(x => x.ShadeID, x => x.ProductID)
                : new Dictionary<int, int>();

            var postedPanels = (panelId ?? new List<string>())
                .Select(v => int.TryParse(v, out var parsed) ? parsed : (int?)null)
                .ToList();

            var postedClasses = (classificationId ?? new List<string>())
                .Select(v => int.TryParse(v, out var parsed) ? parsed : (int?)null)
                .ToList();

            var items = new List<SaleLineRequest>();
            for (int i = 0; i < productId.Count; i++)
            {
                if (productId[i] > 0 && quantity[i] > 0)
                {
                    var shade = i < postedShades.Count ? postedShades[i] : null;
                    var panel = i < postedPanels.Count ? postedPanels[i] : null;
                    var classification = i < postedClasses.Count ? postedClasses[i] : null;

                    // A panel without the class it was priced for says
                    // nothing: the price and the cut size are both keyed on
                    // the pair. Half of it is not half an answer, so neither
                    // is kept.
                    if (panel == null || classification == null)
                    {
                        panel = null;
                        classification = null;
                    }

                    if (shade is { } picked
                        && (!shadeOwners.TryGetValue(picked, out var owner)
                            || owner != productId[i]))
                    {
                        shade = null;
                    }

                    items.Add(new SaleLineRequest
                    {
                        ProductID = productId[i],
                        Quantity = quantity[i],
                        UnitPrice = unitPrice[i],
                        ShadeID = shade,
                        PanelID = panel,
                        VehicleClassificationID = classification
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
            //
            // Two shades of one film are two different things and stay two
            // lines, which is why the shade is part of what has to match.
            items = items
                .GroupBy(i => new { i.ProductID, i.UnitPrice, i.ShadeID, i.PanelID, i.VehicleClassificationID })
                .Select(g => new SaleLineRequest
                {
                    ProductID = g.Key.ProductID,
                    UnitPrice = g.Key.UnitPrice,
                    ShadeID = g.Key.ShadeID,
                    PanelID = g.Key.PanelID,
                    VehicleClassificationID = g.Key.VehicleClassificationID,
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
            //
            // For everybody, administrators included: an administrator changes
            // what the price list says, under Pricing or Products, and the
            // counter sells at that. A line the list has no price for is
            // refused with the record to fill in - see PriceBook.
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

                    // The class the LINE names comes first: a walk-in has
                    // no vehicle, and the price the counter was shown came
                    // from the class it picked.
                    var lineClassification = line.VehicleClassificationID ?? vehicleClassificationId;

                    if (!inTheBook && lineClassification != null)
                    {
                        // The panel's own price, where the line names one -
                        // not any panel's price for this product.
                        inTheBook = await _context.Pricings.AnyAsync(pr =>
                            pr.ProductID == line.ProductID &&
                            pr.VehicleClassificationID == lineClassification &&
                            (line.PanelID == null || pr.PanelID == line.PanelID) &&
                            pr.Price == line.UnitPrice &&
                            pr.Price > 0);
                    }

                    // Nothing in the book for this product at all: say where
                    // its price is set, rather than letting a figure through.
                    var hasAnyPrice = product?.DefaultPrice is > 0m
                        || (lineClassification != null && await _context.Pricings.AnyAsync(pr =>
                                pr.ProductID == line.ProductID &&
                                pr.VehicleClassificationID == lineClassification &&
                                pr.Price > 0));

                    if (!inTheBook && !hasAnyPrice)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"{product?.ProductName ?? "That product"} has no price yet. "
                            + "An administrator sets it under Catalog & Inventory → Pricing "
                            + "(or on the product, for something sold by the piece).");

                        await LoadDropdowns();
                        return View();
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
            // Every tint variant each product has, and the shades under
            // each variant, from the CATALOGUE rather than from the price
            // list - the same source the job order and the invoice use.
            //
            // Taken from the price list, a film with no price row for the
            // class in front of it offered no variant at all, and so no
            // shade either: the boxes went empty exactly when somebody needed
            // to say what they were selling. A variant says what the film IS;
            // only the panel and the price depend on the class of car.
            ViewData["VariantsByProduct"] = await _context.TintVariants
                .OrderBy(v => v.VariantName)
                .Select(v => new
                {
                    productId = v.ProductID,
                    tintVariantId = v.TintVariantID,
                    tintVariantName = v.VariantName
                })
                .ToListAsync();

            ViewData["ShadesByVariant"] = await _context.Shades
                .OrderBy(sh => sh.ShadeName)
                .Select(sh => new
                {
                    tintVariantId = sh.TintVariantID,
                    shadeId = sh.ShadeID,
                    shadeName = sh.ShadeName
                })
                .ToListAsync();

            // The classes of car the price list is keyed by, so a film line
            // can name one without a vehicle on the sale.
            ViewData["Classifications"] = await _context.VehicleClassifications
                .OrderBy(c => c.ClassificationName)
                .Select(c => new { c.VehicleClassificationID, c.ClassificationName })
                .ToListAsync();

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
                            shadeId = sh.ShadeID,
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
