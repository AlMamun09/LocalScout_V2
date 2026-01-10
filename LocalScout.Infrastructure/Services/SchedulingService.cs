using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Service for handling scheduling business logic including validation and overlap detection
    /// </summary>
    public class SchedulingService : ISchedulingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProviderTimeSlotRepository _timeSlotRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<SchedulingService> _logger;

        // Minimum lead time for booking (2 hours)
        private static readonly TimeSpan MinimumLeadTime = TimeSpan.FromHours(2);

        public SchedulingService(
            ApplicationDbContext context,
            IProviderTimeSlotRepository timeSlotRepository,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepository,
            ILogger<SchedulingService> logger)
        {
            _context = context;
            _timeSlotRepository = timeSlotRepository;
            _userManager = userManager;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateBookingTimeAsync(
            string providerId,
            DateTime requestedDate,
            TimeSpan startTime,
            TimeSpan? endTime)
        {
            // 1. Check if date is in the future
            if (requestedDate.Date < DateTime.Now.Date)
            {
                return (false, "Cannot book for past dates.");
            }

            // 2. Check minimum lead time (2 hours)
            if (!ValidateMinimumLeadTime(requestedDate, startTime))
            {
                return (false, "Bookings must be made at least 2 hours in advance.");
            }

            // 3. Check duty hours - only check start time if end time not provided
            var effectiveEndTime = endTime ?? startTime;
            var dutyCheck = await IsWithinProviderDutyHoursAsync(providerId, startTime, effectiveEndTime);
            if (!dutyCheck.IsWithinHours)
            {
                var dutyMessage = dutyCheck.DutyStart.HasValue && dutyCheck.DutyEnd.HasValue
                    ? $"Provider is only available from {FormatTime(dutyCheck.DutyStart.Value)} to {FormatTime(dutyCheck.DutyEnd.Value)}."
                    : "Requested time is outside provider's working hours.";
                return (false, dutyMessage);
            }

            // 4. Check provider availability (overlapping slots) - only if we have meaningful time range
            // For user bookings without end time, we just check if start time conflicts with any locked slot
            var startDateTime = CombineDateAndTime(requestedDate, startTime);
            
            if (endTime.HasValue && endTime.Value != startTime)
            {
                // End time provided - check full range
                var endDateTime = CombineDateAndTime(requestedDate, endTime.Value);
                var availabilityCheck = await CheckProviderAvailabilityAsync(providerId, startDateTime, endDateTime);
                if (!availabilityCheck.IsAvailable)
                {
                    return (false, availabilityCheck.Message);
                }
            }
            else
            {
                // No end time - check if start time falls within any locked slot
                var availabilityCheck = await CheckStartTimeAvailabilityAsync(providerId, startDateTime);
                if (!availabilityCheck.IsAvailable)
                {
                    return (false, availabilityCheck.Message);
                }
            }

            return (true, null);
        }

        /// <summary>
        /// Check if a specific start time falls within any locked time slot
        /// </summary>
        private async Task<(bool IsAvailable, string? Message)> CheckStartTimeAvailabilityAsync(
            string providerId,
            DateTime startDateTime)
        {
            // Check if the start time falls within any active time slot
            var conflictingSlot = await _context.Set<ProviderTimeSlot>()
                .Where(ts => ts.ProviderId == providerId && ts.IsActive)
                .Where(ts => startDateTime >= ts.StartDateTime && startDateTime < ts.EndDateTime)
                .FirstOrDefaultAsync();

            if (conflictingSlot != null)
            {
                return (false, $"Provider is not available at {startDateTime:h:mm tt}. This time is already booked from {conflictingSlot.StartDateTime:h:mm tt} to {conflictingSlot.EndDateTime:h:mm tt}.");
            }

            return (true, null);
        }

        public async Task<(bool IsAvailable, string? Message)> CheckProviderAvailabilityAsync(
            string providerId,
            DateTime startDateTime,
            DateTime endDateTime,
            Guid? excludeBookingId = null)
        {
            var hasOverlap = await _timeSlotRepository.HasOverlappingSlotAsync(
                providerId, startDateTime, endDateTime, excludeBookingId);

            if (hasOverlap)
            {
                var provider = await _userManager.FindByIdAsync(providerId);
                var providerName = provider?.FullName ?? "Provider";
                
                return (false, $"Provider {providerName} is not available from {startDateTime:h:mm tt} to {endDateTime:h:mm tt}. Please select a different time.");
            }

            return (true, null);
        }

        public async Task<(bool IsWithinHours, TimeSpan? DutyStart, TimeSpan? DutyEnd)> IsWithinProviderDutyHoursAsync(
            string providerId,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var dutyHours = await GetProviderDutyHoursAsync(providerId);

            // If no duty hours defined, assume always available
            if (!dutyHours.Start.HasValue || !dutyHours.End.HasValue)
            {
                return (true, null, null);
            }

            var dutyStart = dutyHours.Start.Value;
            var dutyEnd = dutyHours.End.Value;

            // Check if requested time is within duty hours
            // For bookings without end time (endTime == startTime), just check start time
            bool isWithin;
            if (startTime == endTime)
            {
                isWithin = startTime >= dutyStart && startTime < dutyEnd;
            }
            else
            {
                isWithin = startTime >= dutyStart && endTime <= dutyEnd;
            }

            return (isWithin, dutyStart, dutyEnd);
        }

        public async Task<(TimeSpan? Start, TimeSpan? End, string? RawValue)> GetProviderDutyHoursAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            
            if (provider == null || string.IsNullOrEmpty(provider.WorkingHours))
            {
                return (null, null, null);
            }

            // Parse working hours (expected format: "08:00-17:00" or "8:00 AM-5:00 PM")
            try
            {
                var parts = provider.WorkingHours.Split('-', StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    if (TimeSpan.TryParse(parts[0], out var start) && 
                        TimeSpan.TryParse(parts[1], out var end))
                    {
                        return (start, end, provider.WorkingHours);
                    }
                    
                    // Try parsing with AM/PM format
                    if (DateTime.TryParse(parts[0], out var startDt) && 
                        DateTime.TryParse(parts[1], out var endDt))
                    {
                        return (startDt.TimeOfDay, endDt.TimeOfDay, provider.WorkingHours);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse working hours for provider {ProviderId}: {WorkingHours}", 
                    providerId, provider.WorkingHours);
            }

            return (null, null, provider.WorkingHours);
        }

        public async Task<int> ProcessOverlappingRequestsAsync(
            string providerId,
            DateTime startDateTime,
            DateTime endDateTime,
            Guid acceptedBookingId)
        {
            // Find all pending bookings for this provider that overlap with the accepted time
            var overlappingBookings = await _context.Bookings
                .Where(b => b.ProviderId == providerId)
                .Where(b => b.BookingId != acceptedBookingId)
                .Where(b => b.Status == BookingStatus.PendingProviderReview)
                .Where(b => b.RequestedDate.HasValue && 
                           b.RequestedStartTime.HasValue)
                .ToListAsync();

            var affectedCount = 0;
            var provider = await _userManager.FindByIdAsync(providerId);

            foreach (var booking in overlappingBookings)
            {
                var bookingStart = CombineDateAndTime(booking.RequestedDate!.Value, booking.RequestedStartTime!.Value);
                
                // For bookings without end time, use start time + 1 hour as assumed duration
                var bookingEndTime = booking.RequestedEndTime ?? booking.RequestedStartTime!.Value.Add(TimeSpan.FromHours(1));
                var bookingEnd = CombineDateAndTime(booking.RequestedDate!.Value, bookingEndTime);

                // Check if this booking overlaps
                if (bookingStart < endDateTime && bookingEnd > startDateTime)
                {
                    // Move to NeedRescheduling
                    booking.Status = BookingStatus.NeedRescheduling;
                    booking.UpdatedAt = DateTime.Now;
                    affectedCount++;

                    // Notify the user
                    await _notificationRepository.CreateNotificationAsync(
                        booking.UserId,
                        "Booking Needs Rescheduling",
                        $"Your booking request needs rescheduling because {provider?.FullName ?? "the provider"} has accepted another booking for the same time slot. Please propose a new time.",
                        null
                    );
                }
            }

            if (affectedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return affectedCount;
        }

        public bool ValidateMinimumLeadTime(DateTime requestedDate, TimeSpan startTime)
        {
            var requestedDateTime = CombineDateAndTime(requestedDate, startTime);
            var minimumTime = DateTime.Now.Add(MinimumLeadTime);
            
            return requestedDateTime >= minimumTime;
        }

        public DateTime CombineDateAndTime(DateTime date, TimeSpan time)
        {
            return date.Date.Add(time);
        }

        private static string FormatTime(TimeSpan time)
        {
            var dt = DateTime.Today.Add(time);
            return dt.ToString("h:mm tt");
        }
    }
}
