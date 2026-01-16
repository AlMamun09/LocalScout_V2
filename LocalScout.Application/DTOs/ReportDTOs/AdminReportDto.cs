namespace LocalScout.Application.DTOs.ReportDTOs
{
    public class AdminReportDto
    {
        public string DateRange { get; set; } = "last30";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        // User & Provider Stats
        public int TotalUsers { get; set; }
        public int TotalProviders { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveProviders { get; set; }
        public int NewUsersInPeriod { get; set; }
        public int NewProvidersInPeriod { get; set; }
        
        // NEW: Blocked & Pending Stats
        public int BlockedUsers { get; set; }
        public int BlockedProviders { get; set; }
        public int PendingVerifications { get; set; }
        
        // NEW: Service Stats
        public int TotalServices { get; set; }
        public int ActiveServices { get; set; }
        public int NewServicesInPeriod { get; set; }
        
        // Revenue & Booking Stats
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        
        // NEW: Detailed Booking Stats
        public int CancelledBookings { get; set; }
        public int PendingBookings { get; set; }
        public int InProgressBookings { get; set; }
        
        // NEW: Review Stats
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }
}
