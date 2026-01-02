namespace LocalScout.Application.DTOs.ReportDTOs
{
    public class UserBookingReportItem
    {
        public Guid BookingId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
    }
}
