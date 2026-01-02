namespace LocalScout.Application.DTOs.ReportDTOs
{
    public class AdminReportDto
    {
        public string DateRange { get; set; } = "last30";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProviders { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveProviders { get; set; }
        public int NewUsersInPeriod { get; set; }
        public int NewProvidersInPeriod { get; set; }
    }
}
