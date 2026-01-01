using LocalScout.Application.DTOs.PaymentDTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Service for generating payment receipt PDFs
    /// </summary>
    public class ReceiptPdfService
    {
        public byte[] GenerateReceipt(PaymentReceiptDto receipt)
        {
            // Set QuestPDF license (Community license for open source)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(c => ComposeHeader(c, receipt));
                    page.Content().Element(c => ComposeContent(c, receipt));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, PaymentReceiptDto receipt)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Neighbourly")
                            .FontSize(28)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        col.Item().Text("Payment Receipt")
                            .FontSize(16)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(120).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Receipt #{receipt.ReceiptNumber}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                        col.Item().AlignRight().Text(receipt.PaymentDate.ToString("MMM dd, yyyy"))
                            .FontSize(10);
                        col.Item().AlignRight().Text(receipt.PaymentDate.ToString("hh:mm tt"))
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        private void ComposeContent(IContainer container, PaymentReceiptDto receipt)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Payment Status Badge
                column.Item().AlignCenter().Padding(10).Background(Colors.Green.Lighten4)
                    .Border(1).BorderColor(Colors.Green.Darken1)
                    .Text("✓ PAYMENT SUCCESSFUL")
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Green.Darken3);

                column.Item().Height(20);

                // Transaction Details
                column.Item().Text("Transaction Details").FontSize(14).SemiBold();
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    AddTableRow(table, "Transaction ID", receipt.TransactionId);
                    AddTableRow(table, "Validation ID", receipt.ValidationId ?? "N/A");
                    AddTableRow(table, "Payment Method", receipt.PaymentMethod ?? "Online Payment");
                    AddTableRow(table, "Bank Transaction ID", receipt.BankTransactionId ?? "N/A");
                });

                column.Item().Height(20);

                // Service Details
                column.Item().Text("Service Details").FontSize(14).SemiBold();
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    AddTableRow(table, "Service", receipt.ServiceName);
                    AddTableRow(table, "Provider", receipt.ProviderName);
                    AddTableRow(table, "Provider Contact", receipt.ProviderPhone ?? "N/A");
                    AddTableRow(table, "Booking ID", receipt.BookingId.ToString());
                });

                column.Item().Height(20);

                // Customer Details
                column.Item().Text("Customer Details").FontSize(14).SemiBold();
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    AddTableRow(table, "Name", receipt.CustomerName);
                    AddTableRow(table, "Email", receipt.CustomerEmail ?? "N/A");
                    AddTableRow(table, "Phone", receipt.CustomerPhone ?? "N/A");
                });

                column.Item().Height(30);

                // Amount Box
                column.Item().Background(Colors.Blue.Lighten5).Padding(15).Row(row =>
                {
                    row.RelativeItem().Text("Total Amount Paid").FontSize(16).SemiBold();
                    row.ConstantItem(150).AlignRight().Text($"৳ {receipt.Amount:N2}")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                });
            });
        }

        private void AddTableRow(TableDescriptor table, string label, string? value)
        {
            table.Cell().Padding(5).Text(label).FontColor(Colors.Grey.Darken1);
            table.Cell().Padding(5).Text(value ?? "N/A").SemiBold();
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("Thank you for using LocalScout!")
                        .FontSize(10)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(200).AlignRight().Text("This is a computer-generated receipt.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });
            });
        }
    }
}
