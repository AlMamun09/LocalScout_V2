namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider time adjustments on accepted bookings
    /// </summary>
    public class TimeAdjustmentDto
    {
        public Guid BookingId { get; set; }
        public DateTime NewStartDateTime { get; set; }
        public DateTime NewEndDateTime { get; set; }
        public string? Reason { get; set; }
    }
    
    /// <summary>
    /// DTO for countdown clock display on accepted bookings
    /// </summary>
    public class BookingCountdownDto
    {
        public Guid BookingId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public double ProgressPercentage { get; set; }
        public bool IsStarted { get; set; }
        public bool IsCompleted { get; set; }
        public bool CanAdjustTime { get; set; }
    }
}
