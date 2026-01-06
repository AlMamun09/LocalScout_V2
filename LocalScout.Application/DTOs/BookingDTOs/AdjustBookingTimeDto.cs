namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider adjusting a booking time
    /// </summary>
    public class AdjustBookingTimeDto
    {
        public Guid BookingId { get; set; }
        public DateTime NewDate { get; set; }
        public TimeSpan NewStartTime { get; set; }
        public TimeSpan NewEndTime { get; set; }
        public string? Reason { get; set; }
    }
}
