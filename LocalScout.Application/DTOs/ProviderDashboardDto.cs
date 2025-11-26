using LocalScout.Domain.Entities;

namespace LocalScout.Application.DTOs
{
    public class ProviderDashboardDto
    {
        public string ProviderId { get; set; } = string.Empty;
        public VerificationRequest? VerificationRequest { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalBookings { get; set; }
        public int PendingRequestsCount { get; set; }
        public decimal AverageRating { get; set; }
        public List<BookingDto> RecentBookings { get; set; } = new List<BookingDto>();
    }
}
