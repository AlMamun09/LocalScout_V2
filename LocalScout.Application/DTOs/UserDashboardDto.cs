using LocalScout.Application.DTOs.BookingDTOs;

namespace LocalScout.Application.DTOs
{
    public class UserDashboardDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public List<BookingDto> RecentBookings { get; set; } = new List<BookingDto>();
    }
}
