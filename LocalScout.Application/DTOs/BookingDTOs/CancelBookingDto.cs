namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for cancelling a booking
    /// </summary>
    public class CancelBookingDto
    {
        public Guid BookingId { get; set; }
        public string? Reason { get; set; }
    }
}
