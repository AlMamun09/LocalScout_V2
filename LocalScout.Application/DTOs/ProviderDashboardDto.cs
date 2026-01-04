using LocalScout.Application.DTOs.BookingDTOs;
using LocalScout.Domain.Entities;

namespace LocalScout.Application.DTOs
{
    public class ProviderDashboardDto
    {
        public string ProviderId { get; set; } = string.Empty;
        
        // Profile Info
        public string ProviderName { get; set; } = string.Empty;
        public string? BusinessName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        
        // Stats
        public int ActiveServicesCount { get; set; }
        public decimal TotalEarnings { get; set; }
        public int TotalBookings { get; set; }
        public int PendingRequestsCount { get; set; }
        public decimal AverageRating { get; set; }
        
        // Verification
        public VerificationRequest? VerificationRequest { get; set; }
        
        // Recent Bookings
        public List<BookingDto> RecentBookings { get; set; } = new List<BookingDto>();
    }
}
