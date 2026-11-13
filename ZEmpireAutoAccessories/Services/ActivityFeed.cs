using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>One thing somebody did, as an owner would say it.</summary>
    public sealed class ActivityEvent
    {
        public DateTime At { get; init; }
        public string? UserName { get; init; }

        /// <summary>"created invoice INV-2026-000047"</summary>
        public string Headline { get; set; } = "";

        /// <summary>"4 items added · stock updated"</summary>
        public string? Detail { get; set; }

        /// <summary>The record's own page, when it still exists.</summary>
        public string? Url { get; set; }

        /// <summary>Added, Changed, Deleted or Note - for the dot.</summary>
        public string Kind { get; set; } = ActivityLog.Changed;

        /// <summary>Money moved, something was deleted, or a price changed: worth a look.</summary>
        public bool Important { get; set; }

        /// <summary>Why it is flagged, said in a word or two.</summary>
        public string? Flag { get; set; }
    }

    /// <summary>
    /// The dashboard's "recent activity": the log, said the way the shop says
    /// it.
    ///
    /// The log keeps one row per record touched, which is what an audit
    /// needs and not what an owner reads: one invoice with four lines is six
    /// rows - the invoice, four "service invoice detail" rows, the total
    /// changing - and two actions fill the panel. Here the rows that belong
    /// to one document, by one person, within a few minutes, are one event:
    /// "created invoice INV-2026-000047 · 4 items added". The stock that
    /// moved because of it folds in too.
    ///
    /// Events that move money or remove something are flagged, so a refund
    /// is not one more grey line among the stock movements.
    /// </summary>
    public class ActivityFeed
    {
        private static readonly TimeSpan SameAction = TimeSpan.FromMinutes(5);

        // Documents things are part of, and where each one's page is.
        private static readonly Dictionary<string, (string Controller, string Name)> Documents = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ServiceInvoice"] = ("ServiceInvoice", "invoice"),
            ["Sale"] = ("Sales", "sale"),
            ["Quotation"] = ("Quotation", "quotation"),
            ["JobOrder"] = ("JobOrder", "job order"),
            ["VehicleChecklist"] = ("VehicleChecklist", "vehicle checklist"),
            ["InventoryCheck"] = ("InventoryCheck", "stock count"),
            ["Warranty"] = ("Warranty", "warranty"),
            ["Customer"] = ("Customer", "customer"),
            ["Vehicle"] = ("Vehicle", "vehicle"),
            ["Product"] = ("Product", "product"),
            ["Pricing"] = ("Pricing", "price"),
            ["Service"] = ("Service", "service"),
        };

        private readonly ApplicationDbContext _context;

        public ActivityFeed(ApplicationDbContext context) => _context = context;

        public async Task<List<ActivityEvent>> Recent(int count, Func<string, string, string?> urlFor)
        {
            // Enough rows to make `count` events of even a busy morning.
            var rows = await _context.ActivityLogs.AsNoTracking()
                .OrderByDescending(a => a.At).ThenByDescending(a => a.ActivityID)
                .Take(300)
                .ToListAsync();

            // ---- group: one document, one person, a few minutes ----------
            //
            // Stock that a document moved - paying an invoice, completing a
            // job - is written in the same save as the document, so it has
            // the document's own timestamp. A manual stock-in a few seconds
            // later does not, and stays an event of its own.
            var groups = new List<List<ActivityLog>>();
            var keys = new List<(string Entity, string? Key)>();

            foreach (var row in rows)
            {
                var key = DocumentOf(row);
                var isStock = row.Entity == "InventoryTransaction";
                var at = groups.Count > 0 ? groups[^1] : null;

                var joins = at != null
                    && at[0].UserName == row.UserName
                    && at[^1].At - row.At <= SameAction
                    && (keys[^1] == key
                        || (isStock && MovesStock(keys[^1].Entity) && at.Any(r => Same(r.At, row.At)))
                        || (keys[^1].Entity == "Stock" && MovesStock(key.Entity) && at.Any(r => Same(r.At, row.At))));

                if (joins)
                {
                    at!.Add(row);
                    // A group of stock alone, joined by the document that moved
                    // it, is about the document.
                    if (keys[^1].Entity == "Stock" && !isStock)
                        keys[^1] = key;
                    continue;
                }

                if (groups.Count >= count)
                    break;

                groups.Add(new List<ActivityLog> { row });
                keys.Add(key);
            }

            // ---- name the documents: invoice numbers and the like --------
            var labels = await Labels(keys.Select(k => k.Entity == "Stock" ? ("Product", k.Key) : k).Distinct().ToList());

            return groups.Select((g, i) => Describe(g, keys[i], labels, urlFor)).ToList();
        }

        // ---- the document a row belongs to ----------------------------------

        private static (string Entity, string? Key) DocumentOf(ActivityLog row)
        {
            // A document is itself, whatever it belongs to: a warranty that
            // expired is about the warranty, not its job order.
            if (IsDocument(row.Entity))
                return (row.Entity, row.EntityKey);

            if (row.Entity == "InventoryTransaction")
                return ("Stock", Field(row.Changes, "ProductID") ?? row.ParentKey);

            // Written before price history had a parent.
            if (row.Entity == "PriceHistory")
                return ("Pricing", row.Parent == "Pricing" ? row.ParentKey : Field(row.Changes, "PricingID"));

            if (row.Parent != null && IsDocument(row.Parent))
                return (row.Parent, row.ParentKey);

            return (row.Entity, row.EntityKey);
        }

        private static bool MovesStock(string entity) =>
            entity is "ServiceInvoice" or "Sale" or "JobOrder" or "InventoryCheck";

        private static bool Same(DateTime a, DateTime b) => Math.Abs((a - b).TotalSeconds) < 1;

        private static string? Field(string? changes, string field) =>
            Changes(changes).Where(c => c.Field == field).Select(c => c.To).FirstOrDefault();

        private static bool IsDocument(string entity) => Documents.ContainsKey(entity);

        private async Task<Dictionary<(string, string?), string>> Labels(List<(string Entity, string? Key)> docs)
        {
            var labels = new Dictionary<(string, string?), string>();

            List<int> Ids(string entity) => docs.Where(d => d.Entity == entity && int.TryParse(d.Key, out _))
                                                .Select(d => int.Parse(d.Key!)).Distinct().ToList();

            async Task Load<T>(string entity, IQueryable<T> source, Func<T, int> id, Func<T, string?> name, Func<IQueryable<T>, List<int>, IQueryable<T>> filter)
            {
                var ids = Ids(entity);
                if (ids.Count == 0) return;
                foreach (var item in await filter(source, ids).ToListAsync())
                    if (!string.IsNullOrWhiteSpace(name(item)))
                        labels[(entity, id(item).ToString())] = name(item)!;
            }

            await Load("ServiceInvoice", _context.ServiceInvoices.AsNoTracking(), x => x.ServiceInvoiceID, x => x.InvoiceNumber, (q, ids) => q.Where(x => ids.Contains(x.ServiceInvoiceID)));
            await Load("Sale", _context.Sales.AsNoTracking(), x => x.SalesID, x => x.InvoiceNumber, (q, ids) => q.Where(x => ids.Contains(x.SalesID)));
            await Load("Quotation", _context.Quotations.AsNoTracking(), x => x.QuotationID, x => x.QuotationNumber, (q, ids) => q.Where(x => ids.Contains(x.QuotationID)));
            await Load("JobOrder", _context.JobOrders.AsNoTracking(), x => x.JobOrderID, x => x.JobOrderNumber, (q, ids) => q.Where(x => ids.Contains(x.JobOrderID)));
            await Load("Customer", _context.Customers.AsNoTracking(), x => x.CustomerID, x => x.FullName, (q, ids) => q.Where(x => ids.Contains(x.CustomerID)));
            await Load("Product", _context.Products.AsNoTracking(), x => x.ProductID, x => x.ProductName, (q, ids) => q.Where(x => ids.Contains(x.ProductID)));
            await Load("Vehicle", _context.Vehicles.AsNoTracking(), x => x.VehicleID, x => x.PlateNumber, (q, ids) => q.Where(x => ids.Contains(x.VehicleID)));

            // A price has no name of its own: it is for a product, on a panel.
            var priceIds = Ids("Pricing");
            if (priceIds.Count > 0)
                foreach (var price in await _context.Pricings.AsNoTracking()
                             .Where(x => priceIds.Contains(x.PricingID))
                             .Select(x => new { x.PricingID, Product = x.Product.ProductName, Panel = x.Panel.PanelName })
                             .ToListAsync())
                    labels[("Pricing", price.PricingID.ToString())] = $"for {price.Product} · {price.Panel}";

            return labels;
        }

        // ---- saying it --------------------------------------------------------

        private static ActivityEvent Describe(List<ActivityLog> group, (string Entity, string? Key) key,
            Dictionary<(string, string?), string> labels, Func<string, string, string?> urlFor)
        {
            var first = group[0];
            (string Entity, string? Key) doc = key.Entity == "Stock" ? ("Product", key.Key) : key;
            var e = new ActivityEvent { At = first.At, UserName = first.UserName };

            // A sign-in, a failed one, a lockout.
            if (group.All(r => r.Entity == "Account"))
            {
                e.Headline = Lower(first.Summary);
                e.Kind = ActivityLog.Note;
                if (first.Summary.Contains("Failed", StringComparison.OrdinalIgnoreCase)
                    || first.Summary.Contains("Locked", StringComparison.OrdinalIgnoreCase))
                    Flag(e, "sign-in");
                return e;
            }

            var docName = Documents.TryGetValue(doc.Entity, out var d) ? d.Name : ActivityRecorder.Friendly(doc.Entity);
            var label = labels.TryGetValue((doc.Entity, doc.Key), out var l) ? l
                      : LabelFromSummary(group.FirstOrDefault(r => r.Entity == doc.Entity && r.EntityKey == doc.Key)?.Summary, docName)
                      ?? (doc.Key != null ? "#" + doc.Key : "");

            var self = group.Where(r => r.Entity == doc.Entity && r.EntityKey == doc.Key).ToList();
            var parts = new List<string>();

            // The document itself: made, removed, or changed - and what of it
            // an owner would care about.
            string verb;
            if (self.Any(r => r.Action == ActivityLog.Added) && self.Any(r => r.Action == ActivityLog.Deleted))
            { verb = "added and then removed"; e.Kind = ActivityLog.Deleted; Flag(e, "deleted"); }
            else if (self.Any(r => r.Action == ActivityLog.Added)) { verb = "created"; e.Kind = ActivityLog.Added; }
            else if (self.Any(r => r.Action == ActivityLog.Deleted)) { verb = "deleted"; e.Kind = ActivityLog.Deleted; Flag(e, "deleted"); }
            else { verb = "updated"; e.Kind = ActivityLog.Changed; }

            foreach (var row in self.Where(r => r.Action == ActivityLog.Changed))
            {
                foreach (var (field, from, to) in Changes(row.Changes))
                {
                    if (field is "Status" or "WarrantyStatus")
                    {
                        parts.Add($"marked {to}");
                        if (to is "Cancelled" or "Refunded" or "Voided") Flag(e, to.ToLowerInvariant());
                    }
                    else if (field == "Price" && doc.Entity == "Pricing")
                    {
                        parts.Add($"price {Peso(from)} → {Peso(to)}");
                        Flag(e, "price change");
                    }
                }
            }

            if (doc.Entity == "Pricing" && self.Any(r => r.Action == ActivityLog.Deleted))
                Flag(e, "price removed");

            // Its parts.
            Count(group, doc, "Detail", ActivityLog.Added, "added", parts, "item");
            Count(group, doc, "Detail", ActivityLog.Deleted, "removed", parts, "item");
            Count(group, doc, "Detail", ActivityLog.Changed, "changed", parts, "item");

            foreach (var pay in group.Where(r => r.Entity == "ServiceInvoicePayment" && r.Action == ActivityLog.Added))
            {
                var fields = Changes(pay.Changes).ToDictionary(c => c.Field, c => c.To);
                var amount = fields.TryGetValue("Amount", out var a) ? a : null;
                var refund = fields.TryGetValue("Kind", out var k) && k != "Payment";
                parts.Insert(0, refund ? $"returned {Peso(amount?.TrimStart('-'))}"
                                       : $"took {Peso(amount)}" + (fields.TryGetValue("Notes", out var n) && !string.IsNullOrEmpty(n) ? $" ({n.ToLowerInvariant()})" : ""));
                Flag(e, refund ? "refund" : "payment");
                if (verb == "updated") verb = refund ? "refunded" : "took a payment on";
            }

            foreach (var note in group.Where(r => r.Action == ActivityLog.Note))
                parts.Add(Lower(note.Summary));

            var stock = group.Count(r => r.Entity == "InventoryTransaction");
            if (stock > 0)
                parts.Add("stock updated");

            // A product that is only stock moving reads as the movement.
            if (key.Entity == "Stock")
            {
                e.Headline = $"moved stock for {label}";
                e.Detail = StockWords(group);
                e.Kind = ActivityLog.Changed;
                e.Url = urlFor("Inventory", doc.Key ?? "");
                return e;
            }

            // "updated" with nothing to say about the document itself, but
            // parts changed: say the parts.
            if (verb == "updated" && parts.Count > 0 && !self.Any())
                verb = "worked on";

            e.Headline = $"{verb} {docName} {label}".Trim();
            e.Detail = parts.Count > 0 ? string.Join(" · ", parts.Distinct()) : null;
            e.Url = e.Kind == ActivityLog.Deleted || !Documents.ContainsKey(doc.Entity) || doc.Key == null
                ? null
                : urlFor(Documents[doc.Entity].Controller, doc.Key);
            return e;
        }

        private static void Count(List<ActivityLog> group, (string Entity, string? Key) doc, string suffix,
                                  string action, string verb, List<string> parts, string noun)
        {
            var n = group.Count(r => r.Entity.EndsWith(suffix) && r.Action == action
                                     && r.Parent == doc.Entity && r.ParentKey == doc.Key);
            if (n > 0)
                parts.Add($"{n} {noun}{(n == 1 ? "" : "s")} {verb}");
        }

        private static string StockWords(List<ActivityLog> group)
        {
            var ins = 0; var outs = 0;
            foreach (var row in group)
            {
                var type = Changes(row.Changes).FirstOrDefault(c => c.Field == "TransactionType").To;
                if (type == "IN") ins++; else if (type == "OUT") outs++;
            }
            var words = new List<string>();
            if (ins > 0) words.Add(ins == 1 ? "stocked in" : $"{ins} stock-ins");
            if (outs > 0) words.Add(outs == 1 ? "stocked out" : $"{outs} stock-outs");
            return words.Count > 0 ? string.Join(", ", words) : "stock moved";
        }

        private static void Flag(ActivityEvent e, string why)
        {
            e.Important = true;
            e.Flag ??= why;
        }

        // "Changed service invoice INV-2026-000047" -> "INV-2026-000047"
        private static string? LabelFromSummary(string? summary, string docName)
        {
            if (summary == null) return null;
            var m = Regex.Match(summary, @"^(Added|Changed|Deleted)\s+.+?\s+(?<label>[A-Z]{2,}[-\w]*|#\d+|.+)$");
            return m.Success ? m.Groups["label"].Value : null;
        }

        private static IEnumerable<(string Field, string? From, string? To)> Changes(string? changes)
        {
            if (string.IsNullOrEmpty(changes)) yield break;
            foreach (var line in changes.Split('\n'))
            {
                var colon = line.IndexOf(": ", StringComparison.Ordinal);
                if (colon < 0) continue;
                var field = line[..colon];
                var value = line[(colon + 2)..];
                var arrow = value.IndexOf(" → ", StringComparison.Ordinal);
                yield return arrow >= 0
                    ? (field, value[..arrow], value[(arrow + 3)..])
                    : (field, null, value);
            }
        }

        private static string Peso(string? amount) =>
            decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                ? Money.Peso(v)
                : amount ?? "";

        private static string Lower(string text) =>
            text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
    }
}
