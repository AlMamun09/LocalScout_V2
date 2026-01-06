namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for time slot validation request (AJAX)
    /// </summary>
    public class TimeValidationRequestDto
    {
        public string ProviderId { get; set; } = string.Empty;
        public Guid ServiceId { get; set; }
        public DateTime RequestedDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
    
    /// <summary>
    /// DTO for time slot validation response (AJAX)
    /// </summary>
    public class TimeValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsProviderAvailable { get; set; }
        public bool IsWithinDutyHours { get; set; }
        public bool HasMinimumLeadTime { get; set; }
        public string? DutyHoursMessage { get; set; }
    }
}
