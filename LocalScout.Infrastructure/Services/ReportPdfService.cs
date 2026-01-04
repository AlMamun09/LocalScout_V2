using LocalScout.Application.DTOs.ReportDTOs;
using LocalScout.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Service for generating PDF reports using QuestPDF
    /// </summary>
    public class ReportPdfService : IReportPdfService
    {
        public ReportPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region User Report

        public byte[] GenerateUserReport(UserReportDto report, string userName)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, "Activity Report", userName, report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeUserContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeUserContent(IContainer container, UserReportDto report)
        {
            container.PaddingVertical(15).Column(column =>
            {
                // Summary Cards
                column.Item().Text("Summary").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Green.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Cancelled", report.CancelledBookings.ToString(), Colors.Red.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Spent", $"৳{report.TotalSpent:N0}", Colors.Amber.Lighten4));
                });

                column.Item().Height(20);

                // Bookings Table
                if (report.Bookings.Any())
                {
                    column.Item().Text("Booking Details").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Service
                            columns.RelativeColumn(2); // Provider
                            columns.RelativeColumn(1.5f); // Date
                            columns.RelativeColumn(1); // Status
                            columns.RelativeColumn(1); // Amount
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Service").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Provider").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Date").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Status").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Amount (Tk)").FontColor(Colors.White).Bold();
                        });

                        foreach (var booking in report.Bookings)
                        {
                            var bgColor = report.Bookings.IndexOf(booking) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            table.Cell().Background(bgColor).Padding(5).Text(booking.ServiceName);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.ProviderName);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.BookingDate.ToString("MMM dd, yyyy"));
                            table.Cell().Background(bgColor).Padding(5).Text(booking.Status);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.Amount.HasValue ? $"Tk {booking.Amount.Value:N0}" : "-");
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(20).AlignCenter().Text("No bookings found for this period.")
                        .FontColor(Colors.Grey.Darken1).Italic();
                }
            });
        }

        #endregion

        #region Provider Report

        public byte[] GenerateProviderReport(ProviderReportDto report, string providerName)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, "Earnings Report", providerName, report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeProviderContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeProviderContent(IContainer container, ProviderReportDto report)
        {
            container.PaddingVertical(15).Column(column =>
            {
                // Summary Cards - Row 1
                column.Item().Text("Performance Summary").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Green.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Pending", report.PendingBookings.ToString(), Colors.Orange.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Cancelled", report.CancelledBookings.ToString(), Colors.Red.Lighten4));
                });

                column.Item().Height(10);

                // Summary Cards - Row 2
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Earnings", $"৳{report.TotalEarnings:N0}", Colors.Teal.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Avg Rating", $"★ {report.AverageRating:F1}", Colors.Amber.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Reviews", report.TotalReviews.ToString(), Colors.Purple.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Empty spacer
                });

                column.Item().Height(20);

                // Bookings Table
                if (report.Bookings.Any())
                {
                    column.Item().Text("Booking Details").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Service
                            columns.RelativeColumn(2); // Customer
                            columns.RelativeColumn(1.5f); // Date
                            columns.RelativeColumn(1); // Status
                            columns.RelativeColumn(1); // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).Text("Service").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).Text("Customer").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).Text("Date").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).Text("Status").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Teal.Darken2).Padding(5).Text("Amount (Tk)").FontColor(Colors.White).Bold();
                        });

                        foreach (var booking in report.Bookings)
                        {
                            var bgColor = report.Bookings.IndexOf(booking) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            table.Cell().Background(bgColor).Padding(5).Text(booking.ServiceName);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.CustomerName);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.BookingDate.ToString("MMM dd, yyyy"));
                            table.Cell().Background(bgColor).Padding(5).Text(booking.Status);
                            table.Cell().Background(bgColor).Padding(5).Text(booking.Amount.HasValue ? $"Tk {booking.Amount.Value:N0}" : "-");
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(20).AlignCenter().Text("No bookings found for this period.")
                        .FontColor(Colors.Grey.Darken1).Italic();
                }
            });
        }

        #endregion

        #region Admin Report

        public byte[] GenerateAdminReport(AdminReportDto report)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, "System Report", "Administrator", report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeAdminContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeAdminContent(IContainer container, AdminReportDto report)
        {
            container.PaddingVertical(15).Column(column =>
            {
                // User Statistics
                column.Item().Text("User Statistics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Users", report.TotalUsers.ToString(), Colors.Blue.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Active Users", report.ActiveUsers.ToString(), Colors.Green.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "New in Period", report.NewUsersInPeriod.ToString(), Colors.Cyan.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                });

                column.Item().Height(20);

                // Provider Statistics
                column.Item().Text("Provider Statistics").FontSize(14).SemiBold().FontColor(Colors.Teal.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Providers", report.TotalProviders.ToString(), Colors.Teal.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Active Providers", report.ActiveProviders.ToString(), Colors.Green.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "New in Period", report.NewProvidersInPeriod.ToString(), Colors.Cyan.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                });
                column.Item().Height(20);

                // Revenue & Bookings
                column.Item().Text("Revenue & Bookings").FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Revenue", $"৳{report.TotalRevenue:N0}", Colors.Green.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Teal.Lighten4));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                });

                column.Item().Height(30);

                // Summary Table
                column.Item().Text("Summary").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    AddSummaryRow(table, "Total Registered Users", report.TotalUsers.ToString());
                    AddSummaryRow(table, "Active Users", report.ActiveUsers.ToString());
                    AddSummaryRow(table, "New Users (in period)", report.NewUsersInPeriod.ToString());
                    AddSummaryRow(table, "Total Registered Providers", report.TotalProviders.ToString());
                    AddSummaryRow(table, "Active & Verified Providers", report.ActiveProviders.ToString());
                    AddSummaryRow(table, "New Providers (in period)", report.NewProvidersInPeriod.ToString());
                });
            });
        }

        private void AddSummaryRow(TableDescriptor table, string label, string value)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(label);
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text(value).SemiBold();
        }

        #endregion

        #region Shared Components

        private void ComposeHeader(IContainer container, string reportTitle, string userName, DateTime startDate, DateTime endDate)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Neighbourly")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        col.Item().Text(reportTitle)
                            .FontSize(16)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(150).Column(col =>
                    {
                        col.Item().AlignRight().Text(userName)
                            .FontSize(11)
                            .SemiBold();
                        col.Item().AlignRight().Text($"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        private void StatCard(IContainer container, string title, string value, string bgColor)
        {
            container.Background(bgColor).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
            {
                col.Item().Text(title).FontSize(9).FontColor(Colors.Grey.Darken2);
                col.Item().Text(value).FontSize(16).Bold().FontColor(Colors.Grey.Darken3);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Generated on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                    row.ConstantItem(200).AlignRight().Text("Neighbourly - LocalScout")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });
            });
        }

        #endregion
    }
}
