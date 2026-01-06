namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for creating a new booking request
    /// </summary>
    public class CreateBookingDto
    {
        public Guid ServiceId { get; set; }
        public string? Description { get; set; }
        public string? AddressArea { get; set; }
        public List<string>? ImagePaths { get; set; }
        
        // Scheduling fields (required for new bookings)
        public DateTime RequestedDate { get; set; }
        public TimeSpan RequestedStartTime { get; set; }
        public TimeSpan RequestedEndTime { get; set; }
    }
}
