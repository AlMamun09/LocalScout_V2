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
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c => ComposeHeader(c, "User Activity Report", userName, report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeUserContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeUserContent(IContainer container, UserReportDto report)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Summary Section
                column.Item().Text("Activity Summary").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Spent", $"Tk {report.TotalSpent:N0}", Colors.Amber.Lighten5, Colors.Amber.Darken3));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Cancelled", report.CancelledBookings.ToString(), Colors.Red.Lighten5, Colors.Red.Darken2));
                });
                
                // Extra Row for Pending/InProgress
                var pending = report.Bookings.Count(b => b.Status == "PendingProviderReview" || b.Status == "PaymentReceived");
                var inProgress = report.Bookings.Count(b => b.Status == "InProgress");

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Pending", pending.ToString(), Colors.Orange.Lighten5, Colors.Orange.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "In Progress", inProgress.ToString(), Colors.Cyan.Lighten5, Colors.Cyan.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                });

                column.Item().Height(25);

                // Bookings Table
                if (report.Bookings.Any())
                {
                    column.Item().Text("Booking History").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(2); // Service
                            columns.RelativeColumn(2); // Provider
                            columns.RelativeColumn(1.5f); // Date
                            columns.RelativeColumn(1.2f); // Status
                            columns.RelativeColumn(1); // Amount
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(BlockHeader).Text("#");
                            header.Cell().Element(BlockHeader).Text("Service");
                            header.Cell().Element(BlockHeader).Text("Provider");
                            header.Cell().Element(BlockHeader).Text("Date");
                            header.Cell().Element(BlockHeader).Text("Status");
                            header.Cell().Element(BlockHeader).AlignRight().Text("Amount");
                        });

                        var index = 1;
                        foreach (var booking in report.Bookings)
                        {
                            var bgColor = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(index.ToString());
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.ServiceName).SemiBold();
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.ProviderName);
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.BookingDate.ToString("MMM dd, yyyy"));
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.Status).FontSize(9);
                            table.Cell().Element(c => BlockCell(c, bgColor)).AlignRight().Text(booking.Amount.HasValue ? $"Tk {booking.Amount.Value:N0}" : "-");

                            index++;
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten5).Padding(20).AlignCenter()
                        .Text("No booking activity found for this period.")
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
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c => ComposeHeader(c, "Provider Performance Report", providerName, report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeProviderContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeProviderContent(IContainer container, ProviderReportDto report)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // Summary Cards - Row 1
                column.Item().Text("Performance Summary").FontSize(14).SemiBold().FontColor(Colors.Teal.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Earnings", $"Tk {report.TotalEarnings:N0}", Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Teal.Lighten5, Colors.Teal.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Reviews", report.TotalReviews.ToString(), Colors.Purple.Lighten5, Colors.Purple.Darken2));
                });

                column.Item().Height(10);

                // Summary Cards - Row 2
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Average Rating", $"{report.AverageRating:F1} / 5", Colors.Amber.Lighten5, Colors.Amber.Darken3));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Pending", report.PendingBookings.ToString(), Colors.Orange.Lighten5, Colors.Orange.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Cancelled", report.CancelledBookings.ToString(), Colors.Red.Lighten5, Colors.Red.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem(); // Spacer
                });

                column.Item().Height(25);

                // Bookings Table
                if (report.Bookings.Any())
                {
                    column.Item().Text("Booking Details").FontSize(14).SemiBold().FontColor(Colors.Teal.Darken2);
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(2); // Service
                            columns.RelativeColumn(2); // Customer
                            columns.RelativeColumn(1.5f); // Date
                            columns.RelativeColumn(1.2f); // Status
                            columns.RelativeColumn(1); // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(BlockHeader).Text("#");
                            header.Cell().Element(BlockHeader).Text("Service");
                            header.Cell().Element(BlockHeader).Text("Customer");
                            header.Cell().Element(BlockHeader).Text("Date");
                            header.Cell().Element(BlockHeader).Text("Status");
                            header.Cell().Element(BlockHeader).AlignRight().Text("Earnings");
                        });

                        var index = 1;
                        foreach (var booking in report.Bookings)
                        {
                            var bgColor = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(index.ToString());
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.ServiceName).SemiBold();
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.CustomerName);
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.BookingDate.ToString("MMM dd, yyyy"));
                            table.Cell().Element(c => BlockCell(c, bgColor)).Text(booking.Status).FontSize(9);
                            table.Cell().Element(c => BlockCell(c, bgColor)).AlignRight().Text(booking.Amount.HasValue ? $"Tk {booking.Amount.Value:N0}" : "-");
                            
                            index++;
                        }
                    });
                }
                else
                {
                    column.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten5).Padding(20).AlignCenter()
                        .Text("No booking activity found for this period.")
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
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c => ComposeHeader(c, "System Overview Report", "Administrator", report.StartDate, report.EndDate));
                    page.Content().Element(c => ComposeAdminContent(c, report));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeAdminContent(IContainer container, AdminReportDto report)
        {
            container.PaddingVertical(20).Column(column =>
            {
                // User Statistics
                column.Item().Text("User Statistics").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Users", report.TotalUsers.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Active Users", report.ActiveUsers.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "New (Period)", report.NewUsersInPeriod.ToString(), Colors.Cyan.Lighten5, Colors.Cyan.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Blocked", report.BlockedUsers.ToString(), Colors.Red.Lighten5, Colors.Red.Darken2));
                });

                column.Item().Height(20);

                // Provider Statistics
                column.Item().Text("Provider Statistics").FontSize(14).SemiBold().FontColor(Colors.Teal.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Providers", report.TotalProviders.ToString(), Colors.Teal.Lighten5, Colors.Teal.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Verified", report.ActiveProviders.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "New (Period)", report.NewProvidersInPeriod.ToString(), Colors.Cyan.Lighten5, Colors.Cyan.Darken2));
                });
                 column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Pending Verification", report.PendingVerifications.ToString(), Colors.Orange.Lighten5, Colors.Orange.Darken2));
                    row.ConstantItem(10);
                     row.RelativeItem().Element(c => StatCard(c, "Blocked", report.BlockedProviders.ToString(), Colors.Red.Lighten5, Colors.Red.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem();
                    row.ConstantItem(10);
                    row.RelativeItem();
                });

                column.Item().Height(20);

                // Services Statistics
                column.Item().Text("Services Overview").FontSize(14).SemiBold().FontColor(Colors.Purple.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Services", report.TotalServices.ToString(), Colors.Purple.Lighten5, Colors.Purple.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Active", report.ActiveServices.ToString(), Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "New (Period)", report.NewServicesInPeriod.ToString(), Colors.Cyan.Lighten5, Colors.Cyan.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem();
                });

                column.Item().Height(20);

                // Revenue & Bookings
                column.Item().Text("Revenue & Bookings").FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Total Revenue", $"Tk {report.TotalRevenue:N0}", Colors.Green.Lighten5, Colors.Green.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Total Bookings", report.TotalBookings.ToString(), Colors.Blue.Lighten5, Colors.Blue.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Completed", report.CompletedBookings.ToString(), Colors.Teal.Lighten5, Colors.Teal.Darken2));
                });
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Element(c => StatCard(c, "Cancelled", report.CancelledBookings.ToString(), Colors.Red.Lighten5, Colors.Red.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "Pending", report.PendingBookings.ToString(), Colors.Orange.Lighten5, Colors.Orange.Darken2));
                     row.ConstantItem(10);
                    row.RelativeItem().Element(c => StatCard(c, "In Progress", report.InProgressBookings.ToString(), Colors.Cyan.Lighten5, Colors.Cyan.Darken2));
                    row.ConstantItem(10);
                    row.RelativeItem();
                });

                column.Item().Height(30);

                // Summary Table
                column.Item().Text("Executive Summary").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken3);
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                    });

                    AddSummaryRow(table, "Total Registered Users", report.TotalUsers.ToString());
                    AddSummaryRow(table, "Active Users", report.ActiveUsers.ToString());
                    AddSummaryRow(table, "Blocked Users", report.BlockedUsers.ToString());
                    AddSummaryRow(table, "Total Registered Providers", report.TotalProviders.ToString());
                    AddSummaryRow(table, "Active & Verified Providers", report.ActiveProviders.ToString());
                    AddSummaryRow(table, "Pending Verifications", report.PendingVerifications.ToString());
                    AddSummaryRow(table, "Total Services", report.TotalServices.ToString());
                    AddSummaryRow(table, "Total Bookings (in period)", report.TotalBookings.ToString());
                    AddSummaryRow(table, "Total Revenue (in period)", $"Tk {report.TotalRevenue:N0}");
                    AddSummaryRow(table, "Average Platform Rating", $"{report.AverageRating}/5 ({report.TotalReviews} reviews)");
                });
            });
        }

        private void AddSummaryRow(TableDescriptor table, string label, string value)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(8).Text(label).FontSize(10);
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(8).AlignRight().Text(value).SemiBold().FontSize(10);
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
                            .FontColor(Colors.Blue.Darken3);
                        col.Item().Text(reportTitle)
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().AlignRight().Text(userName)
                            .FontSize(11)
                            .SemiBold();
                        col.Item().AlignRight().Text($"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item().PaddingTop(10).PaddingBottom(5).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
            });
        }

        private void StatCard(IContainer container, string title, string value, string bgColor, string accentColor)
        {
            container.Background(bgColor).BorderLeft(4).BorderColor(accentColor).Padding(10).Column(col =>
            {
                col.Item().Text(title).FontSize(9).FontColor(Colors.Grey.Darken2);
                col.Item().Text(value).FontSize(14).Bold().FontColor(Colors.Grey.Darken4);
            });
        }

        private static IContainer BlockHeader(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken3)
                .Padding(5);
        }
        
        // Extension method for styled table header text
        // Note: Using a helper for the cell container, text styling needs to be applied after
        static IContainer BlockCell(IContainer container, string backgroundColor)
        {
            return container
                .Background(backgroundColor)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(5);
        }

        // Helper to apply text style to header cells (QuestPDF text styling is fluent on the Text descriptor)
        // Since we are inside the .Header(header => ...), we can't easily make a global helper for "Cell + Text". 
        // We set styles inline in the table definition.

        private void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten3);
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text(x =>
                    {
                        x.Span("Generated on ").FontColor(Colors.Grey.Medium).FontSize(8);
                        x.Span($"{DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}").FontColor(Colors.Grey.Darken1).FontSize(8);
                    });
                     
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontColor(Colors.Grey.Medium).FontSize(8);
                        x.CurrentPageNumber().FontColor(Colors.Grey.Darken1).FontSize(8);
                        x.Span(" of ").FontColor(Colors.Grey.Medium).FontSize(8);
                        x.TotalPages().FontColor(Colors.Grey.Darken1).FontSize(8);
                    });
                });
            });
        }

        #endregion
    }
}
