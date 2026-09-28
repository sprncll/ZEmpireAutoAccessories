using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZEmpireAutoAccessories.Models;
using ZEmpireAutoAccessories.Services.Interfaces;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Builds a single printable document (quotation, receipt, service
    /// invoice, or job order) for one customer's record - distinct from
    /// ReportPdfBuilder, which renders multi-row report listings.
    /// </summary>
    public static class DocumentPdfBuilder
    {
        private static readonly string BrandRed = "#e5464d";

        public static byte[] BuildQuotationPdf(Quotation quotation, ICutSizeStore? cutSizes = null)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeDocHeader(c, "QUOTATION", quotation.QuotationNumber,
                        quotation.QuotationDate, quotation.Status));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => BillTo(c, quotation.Customer.FullName, quotation.Customer.ContactNumber,
                                quotation.Vehicle.PlateNumber, quotation.Vehicle.Brand, quotation.Vehicle.Model));

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Details").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                                if (quotation.JobType != null)
                                    KeyValue(col, "Job Type", quotation.JobType.JobTypeName);
                                if (quotation.ValidUntil.HasValue)
                                    KeyValue(col, "Valid Until", quotation.ValidUntil.Value.ToString("MMM d, yyyy"));
                                KeyValue(col, "Prepared By", quotation.User.FullName);
                            });
                        });

                        ComposeLineItemsTable(column, quotation.Details.Select(d => new LineItem
                        {
                            Description = d.Product?.ProductName ?? d.Service?.ServiceName ?? d.Description ?? "—",
                            Note = LineNote(
                                d.Product,
                                fitted: true,
                                d.TintVariant?.VariantName,
                                null,
                                d.Panel?.PanelName,
                                Typed(d.Description, d.Product?.ProductName, d.Service?.ServiceName)),
                            Qty = d.Quantity,
                            Unit = UnitFor(d.Product, d.PanelID, d.Unit, d.Quantity),
                            Measure = Measure(cutSizes, d.Product, d.PanelID,
                                              quotation.Vehicle?.VehicleClassificationID, d.Quantity),
                            UnitPrice = d.UnitPrice,
                            Discount = null,
                            SubTotal = d.SubTotal
                        }), includeDiscountColumn: false);

                        ComposeTotals(column, WithVat(
                            ("Sub Total", quotation.SubTotal),
                            ("Discount", -quotation.DiscountAmount),
                            ("Tax entered separately", quotation.TaxAmount),
                            ("Total", quotation.TotalAmount)));

                        if (!string.IsNullOrWhiteSpace(quotation.Remarks))
                            ComposeRemarks(column, quotation.Remarks);
                    });

                    page.Footer().Element(c => ComposeFooter(c, "This quotation is an estimate and does not constitute a final invoice."));
                });
            }).GeneratePdf();
        }

        public static byte[] BuildSalePdf(Sale sale)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeDocHeader(c, "SALES RECEIPT", sale.InvoiceNumber, sale.SalesDate, null));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => BillTo(c, sale.Customer.FullName, sale.Customer.ContactNumber,
                                sale.Vehicle?.PlateNumber, sale.Vehicle?.Brand, sale.Vehicle?.Model));

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Details").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                                KeyValue(col, "Payment Mode", sale.PaymentMode.PaymentModeName);
                                KeyValue(col, "Sold By", sale.User.FullName);
                            });
                        });

                        // The unit column earns its place here: a sale line for
                        // film is written in metres (CreateSale converts it),
                        // so a bare "3" beside a roll product says nothing.
                        ComposeLineItemsTable(column, sale.SaleDetails.Select(d => new LineItem
                        {
                            Description = d.Product.ProductName,
                            // A sale naming a vehicle is film going onto that
                            // car; one without is goods over the counter.
                            Note = LineNote(d.Product, fitted: sale.Vehicle != null),
                            Qty = d.Quantity,
                            // A counter sale has no panel, so film is the
                            // length that was written - in metres.
                            Unit = SoldByLength(d.Product) ? UnitOfMeasure.Meter : UnitOfMeasure.DefaultCountUnit,
                            UnitPrice = d.UnitPrice,
                            Discount = null,
                            SubTotal = d.SubTotal
                        }), includeDiscountColumn: false);

                        ComposeTotals(column, WithVat(("Total", sale.TotalAmount)));
                    });

                    page.Footer().Element(c => ComposeFooter(c, "Thank you for your business!"));
                });
            }).GeneratePdf();
        }

        public static byte[] BuildServiceInvoicePdf(ServiceInvoice invoice, ICutSizeStore? cutSizes = null)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeDocHeader(c, "SERVICE INVOICE", invoice.InvoiceNumber, invoice.InvoiceDate, invoice.Status));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => BillTo(c, invoice.Customer.FullName, invoice.Customer.ContactNumber,
                                invoice.Vehicle?.PlateNumber, invoice.Vehicle?.Brand, invoice.Vehicle?.Model));

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Details").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                                if (invoice.JobOrder != null)
                                    KeyValue(col, "Job Order", invoice.JobOrder.JobOrderNumber);
                                KeyValue(col, "Payment Mode", invoice.PaymentMode.PaymentModeName);
                                KeyValue(col, "Processed By", invoice.User.FullName);
                            });
                        });

                        ComposeLineItemsTable(column, invoice.Details.Select(d => new LineItem
                        {
                            Description = d.Product?.ProductName ?? d.Service?.ServiceName ?? d.Description,
                            Note = LineNote(
                                d.Product,
                                // A service invoice is work on a car by
                                // definition, so the fitting is always in it.
                                fitted: true,
                                d.TintVariant?.VariantName,
                                d.Shade?.ShadeName,
                                d.Panel?.PanelName,
                                Typed(d.Description, d.Product?.ProductName, d.Service?.ServiceName)),
                            Qty = d.Quantity,
                            Unit = UnitFor(d.Product, d.PanelID, d.Unit, d.Quantity),
                            Measure = Measure(cutSizes, d.Product, d.PanelID,
                                              invoice.Vehicle?.VehicleClassificationID, d.Quantity),
                            UnitPrice = d.UnitPrice,
                            Discount = d.DiscountAmount,
                            SubTotal = d.SubTotal
                        }), includeDiscountColumn: true);

                        ComposeTotals(column, WithVat(
                            ("Sub Total", invoice.SubTotal),
                            ("Discount", -invoice.DiscountAmount),
                            ("Tax entered separately", invoice.TaxAmount),
                            ("Total", invoice.TotalAmount),
                            ("Amount Paid", invoice.AmountPaid),
                            ("Change", invoice.ChangeAmount)));

                        if (!string.IsNullOrWhiteSpace(invoice.Remarks))
                            ComposeRemarks(column, invoice.Remarks);
                    });

                    page.Footer().Element(c => ComposeFooter(c, "Thank you for choosing Z-Empire Auto Accessories!"));
                });
            }).GeneratePdf();
        }

        public static byte[] BuildJobOrderPdf(JobOrder jobOrder, ICutSizeStore? cutSizes = null)
        {
            var total = jobOrder.Details.Sum(d => d.SubTotal);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeDocHeader(c, "JOB ORDER", jobOrder.JobOrderNumber, jobOrder.JobOrderDate, jobOrder.Status));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => BillTo(c, jobOrder.Customer.FullName, jobOrder.Customer.ContactNumber,
                                jobOrder.Vehicle.PlateNumber, jobOrder.Vehicle.Brand, jobOrder.Vehicle.Model));

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Details").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                                if (jobOrder.JobType != null)
                                    KeyValue(col, "Job Type", jobOrder.JobType.JobTypeName);
                                KeyValue(col, "Technician", jobOrder.AssignedEmployee != null
                                    ? $"{jobOrder.AssignedEmployee.FirstName} {jobOrder.AssignedEmployee.LastName}" : "Unassigned");
                                if (jobOrder.InstallationDate.HasValue)
                                    KeyValue(col, "Installation Date", jobOrder.InstallationDate.Value.ToString("MMM d, yyyy"));
                                if (jobOrder.Odometer.HasValue)
                                    KeyValue(col, "Odometer", $"{jobOrder.Odometer} km");
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(jobOrder.Complaint) || !string.IsNullOrWhiteSpace(jobOrder.ReasonForChanging)
                            || !string.IsNullOrWhiteSpace(jobOrder.ExistingFilmShade) || !string.IsNullOrWhiteSpace(jobOrder.SpecialInstruction))
                        {
                            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(col =>
                            {
                                if (!string.IsNullOrWhiteSpace(jobOrder.Complaint))
                                    KeyValue(col, "Complaint", jobOrder.Complaint);
                                if (!string.IsNullOrWhiteSpace(jobOrder.ExistingFilmShade))
                                    KeyValue(col, "Existing Film Shade", jobOrder.ExistingFilmShade);
                                if (!string.IsNullOrWhiteSpace(jobOrder.ReasonForChanging))
                                    KeyValue(col, "Reason for Changing", jobOrder.ReasonForChanging);
                                if (!string.IsNullOrWhiteSpace(jobOrder.SpecialInstruction))
                                    KeyValue(col, "Special Instruction", jobOrder.SpecialInstruction);
                            });
                        }

                        ComposeLineItemsTable(column, jobOrder.Details.Select(d => new LineItem
                        {
                            Description = d.Product?.ProductName ?? d.Service?.ServiceName ?? d.Description,
                            Note = LineNote(
                                d.Product,
                                fitted: true,
                                d.TintVariant?.VariantName,
                                d.Shade?.ShadeName,
                                d.Panel?.PanelName,
                                Typed(d.Description, d.Product?.ProductName, d.Service?.ServiceName)),
                            Qty = d.Quantity,
                            Unit = UnitFor(d.Product, d.PanelID, d.Unit, d.Quantity),
                            Measure = Measure(cutSizes, d.Product, d.PanelID,
                                              jobOrder.Vehicle?.VehicleClassificationID, d.Quantity),
                            UnitPrice = d.UnitPrice,
                            Discount = null,
                            SubTotal = d.SubTotal
                        }), includeDiscountColumn: false);

                        ComposeTotals(column, ("Total", total));

                        column.Item().PaddingTop(24).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().PaddingTop(3).Text("Customer Signature").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(30);
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                                col.Item().PaddingTop(3).Text("Technician Signature").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });

                    page.Footer().Element(c => ComposeFooter(c, null));
                });
            }).GeneratePdf();
        }

        private static void ConfigurePage(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(x => x.FontSize(9.5f));
        }

        private static void ComposeDocHeader(IContainer container, string docType, string docNumber, DateTime docDate, string? status)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Z-Empire Auto Accessories").FontSize(16).Bold();
                        col.Item().Text("Auto Tint & Accessories Shop").FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text(docType).FontSize(14).Bold().FontColor(BrandRed);
                        col.Item().AlignRight().Text($"No. {docNumber}").FontSize(9).SemiBold();
                        col.Item().AlignRight().Text(docDate.ToString("MMM d, yyyy")).FontSize(8).FontColor(Colors.Grey.Medium);
                        if (!string.IsNullOrEmpty(status))
                            col.Item().AlignRight().Text(status).FontSize(8).FontColor(BrandRed).SemiBold();
                    });
                });

                column.Item().PaddingTop(8).PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private static void BillTo(IContainer container, string customerName, string? contactNumber,
            string? plateNumber, string? brand, string? model)
        {
            container.Column(col =>
            {
                col.Item().Text("Bill To").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                col.Item().Text(customerName).FontSize(11).Bold();
                if (!string.IsNullOrEmpty(contactNumber))
                    col.Item().Text(contactNumber).FontSize(9).FontColor(Colors.Grey.Darken1);

                if (!string.IsNullOrEmpty(plateNumber) || !string.IsNullOrEmpty(brand))
                {
                    var vehicleLine = string.Join(" — ", new[] { plateNumber, $"{brand} {model}".Trim() }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    col.Item().PaddingTop(2).Text(vehicleLine).FontSize(9).FontColor(Colors.Grey.Darken1);
                }
            });
        }

        private static void KeyValue(ColumnDescriptor column, string key, string? value)
        {
            column.Item().PaddingTop(1).Row(row =>
            {
                row.ConstantItem(90).Text(key).FontSize(8.5f).FontColor(Colors.Grey.Medium);
                row.RelativeItem().Text(value ?? "—").FontSize(9);
            });
        }

        /// <summary>
        /// What is fitted off a roll - film and tint - is quoted as one price
        /// with the work in it: the shop does not charge separately for
        /// cutting and applying it. Saying so on the line keeps the customer
        /// from wondering where the labour went, and stops anyone reading the
        /// price as material only.
        /// </summary>
        private const string ApplicationIncluded = "Cutting and application included";

        /// <summary>
        /// The small print under a line: what exactly was fitted (which tint,
        /// which shade, which panel), whatever was typed on the line itself,
        /// and a note where the work is included in the price.
        /// </summary>
        /// <param name="fitted">
        /// Whether this line is work on a car rather than goods handed over.
        /// Film bought off the counter to take away has had nothing applied
        /// to it, and a receipt saying the application was included would be
        /// promising something that never happened.
        /// </param>
        private static string? LineNote(
            Product? product,
            bool fitted,
            string? tintVariant = null,
            string? shade = null,
            string? panel = null,
            string? typed = null)
        {
            var parts = new List<string>();

            foreach (var detail in new[] { tintVariant, shade, panel, typed })
            {
                if (!string.IsNullOrWhiteSpace(detail))
                    parts.Add(detail.Trim());
            }

            if (fitted && SoldByLength(product))
                parts.Add(ApplicationIncluded);

            return parts.Count == 0 ? null : string.Join(" · ", parts);
        }

        /// <summary>
        /// What one panel line takes off the roll, written out, or null where
        /// the line is not measured that way - anything counted, film sold by
        /// the metre with no panel against it, or a pairing nobody has set a
        /// cut size for yet.
        /// </summary>
        private static string? Measure(
            ICutSizeStore? cutSizes, Product? product, int? panelId,
            int? vehicleClassificationId, decimal quantity)
        {
            if (cutSizes == null || panelId == null || !SoldByLength(product)
                || vehicleClassificationId == null)
                return null;

            var centimeters = cutSizes.Centimeters(vehicleClassificationId.Value, panelId.Value);

            return centimeters == null
                ? null
                : UnitOfMeasure.Describe(centimeters.Value * quantity, true) + " of film";
        }

        /// <summary>
        /// What to print in the Unit column. A panel line counts panels; the
        /// stored "pc" is left over from before these were measured and says
        /// nothing true about a roll.
        /// </summary>
        private static string UnitFor(
            Product? product, int? panelId, string storedUnit, decimal quantity) =>
            SoldByLength(product) && panelId != null
                ? "panel" + (quantity == 1m ? "" : "s")
                : storedUnit;

        /// <summary>
        /// The line's own wording, unless it only repeats the name already
        /// printed beside it - the add-line form fills the description in
        /// from the product, so most lines carry their own name twice.
        /// </summary>
        private static string? Typed(string? description, params string?[] names)
        {
            // A line with neither a product nor a service is named by its
            // description alone - which is already the Description column, so
            // repeating it underneath prints the same words twice.
            if (names.All(string.IsNullOrWhiteSpace))
                return null;

            return names.Any(n => string.Equals(n?.Trim(), description?.Trim(),
                                                StringComparison.OrdinalIgnoreCase))
                ? null
                : description;
        }

        /// <summary>Whether this product comes off a roll and is measured.</summary>
        private static bool SoldByLength(Product? product) =>
            product != null && UnitOfMeasure.IsSoldByLength(product.Category?.CategoryName);

        /// <summary>
        /// One printed line. A record rather than a seven-field tuple now
        /// that it carries a measure as well - the tuple had reached the
        /// point where the call sites were the only thing naming the fields,
        /// and a string in the wrong position would have compiled.
        /// </summary>
        private sealed record LineItem
        {
            public string? Description { get; init; }
            public string? Note { get; init; }
            public decimal Qty { get; init; }
            public string Unit { get; init; } = "";
            public decimal UnitPrice { get; init; }
            public decimal? Discount { get; init; }
            public decimal SubTotal { get; init; }

            /// <summary>
            /// What the line takes off a roll, where that is not the quantity
            /// as written - "5.05 m of film" under a panel count. Null for
            /// anything counted.
            /// </summary>
            public string? Measure { get; init; }
        }

        private static void ComposeLineItemsTable(
            ColumnDescriptor column,
            IEnumerable<LineItem> lines,
            bool includeDiscountColumn,
            bool includeUnitColumn = true)
        {
            var lineList = lines.ToList();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(1.3f);
                    if (includeUnitColumn)
                        columns.RelativeColumn(1.3f);
                    columns.RelativeColumn(1.8f);
                    if (includeDiscountColumn)
                        columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Description");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    if (includeUnitColumn)
                        header.Cell().Element(HeaderCell).Text("Unit");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Unit Price");
                    if (includeDiscountColumn)
                        header.Cell().Element(HeaderCell).AlignRight().Text("Discount");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Sub Total");
                });

                foreach (var line in lineList)
                {
                    table.Cell().Element(BodyCell).Column(col =>
                    {
                        col.Item().Text(line.Description ?? "—");
                        if (!string.IsNullOrWhiteSpace(line.Note))
                            col.Item().Text(line.Note).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    table.Cell().Element(BodyCell).AlignRight().Text(line.Qty.ToString("0.##"));
                    if (includeUnitColumn)
                        table.Cell().Element(BodyCell).Column(unitColumn =>
                        {
                            unitColumn.Item().Text(line.Unit);

                            // The figure that actually came off the roll. A
                            // panel count alone says nothing about the film.
                            if (!string.IsNullOrWhiteSpace(line.Measure))
                                unitColumn.Item().Text(line.Measure)
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                    // Work that was done and not charged for. Printing P0.00
                    // twice reads like a mistake, or like the customer was
                    // owed something; "Included" says what actually happened.
                    var included = line.UnitPrice == 0m && line.SubTotal == 0m;

                    if (included)
                    {
                        table.Cell().Element(BodyCell).AlignRight()
                            .Text("Included").FontColor(Colors.Grey.Darken1);
                        if (includeDiscountColumn)
                            table.Cell().Element(BodyCell).AlignRight().Text("—").FontColor(Colors.Grey.Medium);
                        table.Cell().Element(BodyCell).AlignRight()
                            .Text("Included").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        table.Cell().Element(BodyCell).AlignRight().Text("₱" + line.UnitPrice.ToString("N2"));
                        if (includeDiscountColumn)
                            table.Cell().Element(BodyCell).AlignRight().Text("₱" + (line.Discount ?? 0).ToString("N2"));
                        table.Cell().Element(BodyCell).AlignRight().Text("₱" + line.SubTotal.ToString("N2"));
                    }
                }

                if (lineList.Count == 0)
                {
                    var columnCount = 3 + (includeUnitColumn ? 1 : 0) + (includeDiscountColumn ? 1 : 0);
                    table.Cell().ColumnSpan((uint)columnCount).Element(BodyCell).AlignCenter()
                        .Text("No line items.").FontColor(Colors.Grey.Medium);
                }
            });
        }

        /// <summary>
        /// The VAT split, added under the Total row, and any zero "Tax entered
        /// separately" line dropped on the way through.
        ///
        /// Prices are quoted VAT-inclusive, so the 12% is worked out of the
        /// total rather than charged on top - see Models/Vat.cs. A document
        /// with no Total row (nothing to divide) comes back untouched.
        /// </summary>
        private static (string Label, decimal Value)[] WithVat(params (string Label, decimal Value)[] rows)
        {
            var kept = rows.Where(r => r.Label != "Tax entered separately" || r.Value > 0).ToList();

            var totalAt = kept.FindIndex(r => r.Label == "Total");
            if (totalAt < 0)
                return kept.ToArray();

            var (vatable, vat) = Vat.Breakdown(kept[totalAt].Value);

            kept.InsertRange(totalAt + 1, new[]
            {
                ("VATable Sales", vatable),
                ($"VAT ({Vat.RateLabel}), included", vat)
            });

            return kept.ToArray();
        }

        private static void ComposeTotals(ColumnDescriptor column, params (string Label, decimal Value)[] rows)
        {
            column.Item().AlignRight().Width(220).Column(col =>
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    var (label, value) = rows[i];
                    var isTotal = rows.Length > 1 && label == "Total";
                    var isLast = i == rows.Length - 1;

                    col.Item().Row(row =>
                    {
                        var labelText = row.RelativeItem().Text(label).FontSize(isTotal ? 10.5f : 9);
                        if (isTotal)
                            labelText.SemiBold();

                        var valueText = row.ConstantItem(110).AlignRight()
                            .Text(value < 0 ? "-₱" + Math.Abs(value).ToString("N2") : "₱" + value.ToString("N2"))
                            .FontSize(isTotal ? 11 : 9);
                        if (isTotal || isLast)
                            valueText.SemiBold();
                    });

                    if (isTotal)
                        col.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                }
            });
        }

        private static void ComposeRemarks(ColumnDescriptor column, string remarks)
        {
            column.Item().PaddingTop(4).Column(col =>
            {
                col.Item().Text("Remarks").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                col.Item().Text(remarks).FontSize(9);
            });
        }

        private static void ComposeFooter(IContainer container, string? note)
        {
            container.Column(column =>
            {
                if (!string.IsNullOrEmpty(note))
                    column.Item().AlignCenter().Text(note).FontSize(8).Italic().FontColor(Colors.Grey.Medium);

                column.Item().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Medium));
                    text.Span($"Generated {DateTime.Now:MMM d, yyyy h:mm tt} · Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }

        private static IContainer HeaderCell(IContainer container) =>
            container.Background(Colors.Black).DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold())
                .PaddingVertical(5).PaddingHorizontal(4);

        private static IContainer BodyCell(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);
    }
}
