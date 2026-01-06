namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Service for handling scheduling business logic including validation and overlap detection
    /// </summary>
    public interface ISchedulingService
    {
        /// <summary>
        /// Validate the requested booking time slot
        /// Returns success status and optional error message
        /// </summary>
        Task<(bool IsValid, string? ErrorMessage)> ValidateBookingTimeAsync(
            string providerId, 
            DateTime requestedDate, 
            TimeSpan startTime, 
            TimeSpan endTime);
        
        /// <summary>
        /// Check if provider is available for the given time range
        /// Returns availability status and message
        /// </summary>
        Task<(bool IsAvailable, string? Message)> CheckProviderAvailabilityAsync(
            string providerId, 
            DateTime startDateTime, 
            DateTime endDateTime,
            Guid? excludeBookingId = null);
        
        /// <summary>
        /// Check if the time is within provider's duty hours
        /// </summary>
        Task<(bool IsWithinHours, TimeSpan? DutyStart, TimeSpan? DutyEnd)> IsWithinProviderDutyHoursAsync(
            string providerId, 
            TimeSpan startTime, 
            TimeSpan endTime);
        
        /// <summary>
        /// Get provider's duty hours
        /// </summary>
        Task<(TimeSpan? Start, TimeSpan? End, string? RawValue)> GetProviderDutyHoursAsync(string providerId);
        
        /// <summary>
        /// Process overlapping pending requests after a booking is accepted
        /// Moves them to NeedRescheduling status
        /// </summary>
        Task<int> ProcessOverlappingRequestsAsync(
            string providerId, 
            DateTime startDateTime, 
            DateTime endDateTime, 
            Guid acceptedBookingId);
        
        /// <summary>
        /// Validate minimum 2-hour lead time for booking
        /// </summary>
        bool ValidateMinimumLeadTime(DateTime requestedDate, TimeSpan startTime);
        
        /// <summary>
        /// Combine date and time into a single DateTime
        /// </summary>
        DateTime CombineDateAndTime(DateTime date, TimeSpan time);
    }
}
