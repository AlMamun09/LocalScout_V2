namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider availability slots
    /// </summary>
    public class ProviderAvailabilityDto
    {
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public TimeSpan? DutyStartTime { get; set; }
        public TimeSpan? DutyEndTime { get; set; }
        public string? WorkingDays { get; set; }
        public List<BookedSlotDto> BookedSlots { get; set; } = new();
    }
    
    /// <summary>
    /// DTO representing a booked time slot
    /// </summary>
    public class BookedSlotDto
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? ServiceName { get; set; }
    }
}
