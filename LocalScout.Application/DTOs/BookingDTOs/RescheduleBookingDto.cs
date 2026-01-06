namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for user rescheduling a booking with a new requested time
    /// </summary>
    public class RescheduleBookingDto
    {
        public Guid BookingId { get; set; }
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedStartTime { get; set; }
        public TimeSpan RequestedEndTime { get; set; }
    }
}
