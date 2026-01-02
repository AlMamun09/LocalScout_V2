namespace LocalScout.Application.DTOs.ReportDTOs
{
    public class UserReportDto
    {
        public string DateRange { get; set; } = "last30";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public List<UserBookingReportItem> Bookings { get; set; } = new();
    }
}
