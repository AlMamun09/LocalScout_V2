namespace LocalScout.Application.DTOs.ReportDTOs
{
    public class ProviderReportDto
    {
        public string DateRange { get; set; } = "last30";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal TotalEarnings { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ProviderBookingReportItem> Bookings { get; set; } = new();
    }
}
