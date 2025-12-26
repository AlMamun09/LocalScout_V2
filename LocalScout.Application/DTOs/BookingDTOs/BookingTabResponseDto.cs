namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for AJAX tab response in My Bookings page
    /// </summary>
    public class BookingTabResponseDto
    {
        public bool Success { get; set; }
        public string? Html { get; set; }
        public int Count { get; set; }
        public string? Message { get; set; }
    }
}
