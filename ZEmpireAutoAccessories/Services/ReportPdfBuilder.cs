using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Builds the downloadable PDF versions of the Sales, Quotation and Job
    /// Order reports using QuestPDF. Pure formatting - all filtering/totals
    /// are computed by ReportController before the data reaches here.
    /// </summary>
    public static class ReportPdfBuilder
    {
        private static readonly string BrandRed = "#e5464d";

        public static byte[] BuildSalesPdf(List<VwSalesSummary> items, DateOnly? from, DateOnly? to, decimal total)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeHeader(c, "Sales Report", BuildRangeSubtitle(from, to, null)));

                    page.Content().Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Invoice #");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Customer");
                                header.Cell().Element(HeaderCell).Text("Payment Mode");
                                header.Cell().Element(HeaderCell).Text("Sold By");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                            });

                            foreach (var item in items)
                            {
                                table.Cell().Element(BodyCell).Text(item.InvoiceNumber);
                                table.Cell().Element(BodyCell).Text(item.SalesDate.ToString("MMM d, yyyy"));
                                table.Cell().Element(BodyCell).Text(item.CustomerName ?? "—");
                                table.Cell().Element(BodyCell).Text(item.PaymentModeName);
                                table.Cell().Element(BodyCell).Text(item.SoldBy ?? "—");
                                table.Cell().Element(BodyCell).AlignRight().Text("₱" + item.RecordedTotal.ToString("N2"));
                            }
                        });

                        ComposeEmptyOrTotal(column, items.Count, "No sales found for this range.", total);
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        public static byte[] BuildQuotationsPdf(List<VwQuotationSummary> items, DateOnly? from, DateOnly? to, string? status, decimal total)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeHeader(c, "Quotations Report", BuildRangeSubtitle(from, to, status)));

                    page.Content().Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Number");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Customer");
                                header.Cell().Element(HeaderCell).Text("Status");
                                header.Cell().Element(HeaderCell).Text("Converted To");
                                header.Cell().Element(HeaderCell).Text("Prepared By");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                            });

                            foreach (var item in items)
                            {
                                table.Cell().Element(BodyCell).Text(item.QuotationNumber);
                                table.Cell().Element(BodyCell).Text(item.QuotationDate.ToString("MMM d, yyyy"));
                                table.Cell().Element(BodyCell).Text(item.CustomerName ?? "—");
                                // The same two states the screens show, from
                                // the job order rather than the stored word -
                                // see Models/QuotationStatuses.cs.
                                table.Cell().Element(BodyCell).Text(
                                    QuotationStatuses.Display(item.Status, item.ConvertedJobOrderID != null));
                                table.Cell().Element(BodyCell).Text(item.ConvertedJobOrderNumber ?? "—");
                                table.Cell().Element(BodyCell).Text(item.PreparedBy ?? "—");
                                table.Cell().Element(BodyCell).AlignRight().Text("₱" + item.TotalAmount.ToString("N2"));
                            }
                        });

                        ComposeEmptyOrTotal(column, items.Count, "No quotations found for this range.", total);
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        public static byte[] BuildJobOrdersPdf(List<VwJobOrderSummary> items, DateOnly? from, DateOnly? to, string? status, decimal total)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeHeader(c, "Job Orders Report", BuildRangeSubtitle(from, to, status)));

                    page.Content().Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Number");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Customer");
                                header.Cell().Element(HeaderCell).Text("Job Type");
                                header.Cell().Element(HeaderCell).Text("Status");
                                header.Cell().Element(HeaderCell).Text("Handled By");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Lines");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                            });

                            foreach (var item in items)
                            {
                                table.Cell().Element(BodyCell).Text(item.JobOrderNumber);
                                table.Cell().Element(BodyCell).Text(item.JobOrderDate.ToString("MMM d, yyyy"));
                                table.Cell().Element(BodyCell).Text(item.CustomerName ?? "—");
                                table.Cell().Element(BodyCell).Text(item.JobTypeName ?? "—");
                                table.Cell().Element(BodyCell).Text(item.Status);
                                table.Cell().Element(BodyCell).Text(item.HandledBy ?? "—");
                                table.Cell().Element(BodyCell).AlignRight().Text(item.LineCount.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text("₱" + item.TotalAmount.ToString("N2"));
                            }
                        });

                        ComposeEmptyOrTotal(column, items.Count, "No job orders found for this range.", total);
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        private static void ConfigurePage(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));
        }

        /// <summary>
        /// What was collected over a range: the transactions, then the two
        /// breakdowns the cashiering screen closes a day out with.
        ///
        /// Takes the same view model that screen renders, so the report and
        /// the counter cannot disagree about a day's takings. The totals are
        /// already cleared for anyone not entitled to them, which is why this
        /// simply prints what it is given.
        /// </summary>
        public static byte[] BuildCollectionsPdf(CashieringViewModel model)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => ComposeHeader(c, "Collections Report",
                        BuildRangeSubtitle(model.DateFrom, model.DateTo, null)));

                    page.Content().Column(column =>
                    {
                        column.Spacing(6);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(2.4f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2.4f);
                                columns.RelativeColumn(1.6f);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Type");
                                header.Cell().Element(HeaderCell).Text("Invoice #");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Customer");
                                header.Cell().Element(HeaderCell).Text("Payment");
                                header.Cell().Element(HeaderCell).Text("Cashier");
                                header.Cell().Element(HeaderCell).Text("Status");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Total");
                            });

                            foreach (var txn in model.Transactions)
                            {
                                table.Cell().Element(BodyCell).Text(
                                    txn.Source == CashierSource.Sale ? "Sale" : "Service");
                                table.Cell().Element(BodyCell).Text(txn.InvoiceNumber);
                                table.Cell().Element(BodyCell).Text(txn.TransactionDate.ToString("MMM d, yyyy"));
                                table.Cell().Element(BodyCell).Text(txn.CustomerName ?? "—");
                                table.Cell().Element(BodyCell).Text(txn.PaymentModeName);
                                table.Cell().Element(BodyCell).Text(txn.CashierName ?? "—");
                                table.Cell().Element(BodyCell).Text(txn.Status);

                                // An uncollected row is listed but is not part
                                // of the takings, and the figure is greyed so
                                // nobody adds it in by eye.
                                var amount = table.Cell().Element(BodyCell).AlignRight()
                                    .Text("₱" + txn.TotalAmount.ToString("N2"));
                                if (!txn.IsCollected)
                                    amount.FontColor(Colors.Grey.Medium);
                            }
                        });

                        if (model.Transactions.Count == 0)
                        {
                            column.Item().PaddingTop(10).AlignCenter()
                                .Text("Nothing was collected in this range.").FontColor(Colors.Grey.Medium);
                        }
                        else if (model.CanSeeTotals)
                        {
                            ComposeEmptyOrTotal(column, model.Transactions.Count,
                                "Nothing was collected in this range.", model.GrandTotal);
                        }
                        else
                        {
                            // The takings are cleared for anyone not entitled
                            // to them, so printing the figure would read as
                            // "nothing was taken" rather than "not for you".
                            column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().PaddingTop(2).AlignRight()
                                .Text("Totals are shown to administrators only.")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        }

                        if (model.ByPaymentMode.Count > 0)
                            ComposeBreakdown(column, "Collected by Payment Mode", "Mode",
                                model.ByPaymentMode.Select(m =>
                                    (m.PaymentModeName, m.TransactionCount, m.Total)));

                        if (model.ByCashier.Count > 0)
                            ComposeBreakdown(column, "Collected by Cashier", "Cashier",
                                model.ByCashier.Select(c =>
                                    (c.CashierName ?? "—", c.TransactionCount, c.Total)));
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        /// <summary>One of the two roll-ups under a collections report.</summary>
        private static void ComposeBreakdown(
            ColumnDescriptor column, string title, string firstHeading,
            IEnumerable<(string Name, int Count, decimal Total)> rows)
        {
            var list = rows.ToList();

            column.Item().PaddingTop(14).Text(title).FontSize(11).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text(firstHeading);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Transactions");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                });

                foreach (var (name, count, total) in list)
                {
                    table.Cell().Element(BodyCell).Text(name);
                    table.Cell().Element(BodyCell).AlignRight().Text(count.ToString("N0"));
                    table.Cell().Element(BodyCell).AlignRight().Text("₱" + total.ToString("N2"));
                }

                // A total under a single line only repeats it.
                if (list.Count > 1)
                {
                    table.Cell().Element(BodyCell).Text("Total").SemiBold();
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(list.Sum(r => r.Count).ToString("N0")).SemiBold();
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text("₱" + list.Sum(r => r.Total).ToString("N2")).SemiBold();
                }
            });
        }

        private static void ComposeHeader(IContainer container, string title, string subtitle)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Z-Empire Auto Accessories").FontSize(15).Bold();
                        col.Item().Text(title).FontSize(12).SemiBold().FontColor(BrandRed);
                        if (!string.IsNullOrEmpty(subtitle))
                            col.Item().PaddingTop(2).Text(subtitle).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(140).AlignRight().Text($"Generated {DateTime.Now:MMM d, yyyy h:mm tt}")
                        .FontSize(7).FontColor(Colors.Grey.Medium);
                });

                column.Item().PaddingTop(8).PaddingBottom(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        }

        private static void ComposeEmptyOrTotal(ColumnDescriptor column, int itemCount, string emptyMessage, decimal total)
        {
            if (itemCount == 0)
            {
                column.Item().PaddingTop(10).AlignCenter().Text(emptyMessage).FontColor(Colors.Grey.Medium);
                return;
            }

            column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().AlignRight().Text($"Total: ₱{total:N2}").Bold().FontSize(11);
        }

        private static IContainer HeaderCell(IContainer container) =>
            container.Background(Colors.Black).DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold())
                .PaddingVertical(5).PaddingHorizontal(4);

        private static IContainer BodyCell(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4);

        private static string BuildRangeSubtitle(DateOnly? from, DateOnly? to, string? status)
        {
            var range = (from, to) switch
            {
                (null, null) => "All dates",
                (not null, null) => $"From {from:MMM d, yyyy}",
                (null, not null) => $"Through {to:MMM d, yyyy}",
                _ => $"{from:MMM d, yyyy} – {to:MMM d, yyyy}"
            };

            return string.IsNullOrEmpty(status) ? range : $"{range} · Status: {status}";
        }
    }
}
