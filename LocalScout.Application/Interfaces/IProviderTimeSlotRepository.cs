using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Repository for managing provider time slots (locked availability after booking acceptance)
    /// </summary>
    public interface IProviderTimeSlotRepository
    {
        /// <summary>
        /// Get a time slot by its ID
        /// </summary>
        Task<ProviderTimeSlot?> GetByIdAsync(Guid timeSlotId);
        
        /// <summary>
        /// Get time slot by booking ID
        /// </summary>
        Task<ProviderTimeSlot?> GetByBookingIdAsync(Guid bookingId);
        
        /// <summary>
        /// Get all active time slots for a provider
        /// </summary>
        Task<List<ProviderTimeSlot>> GetProviderTimeSlotsAsync(string providerId, DateTime? fromDate = null);
        
        /// <summary>
        /// Check if a provider has any overlapping time slots
        /// </summary>
        Task<bool> HasOverlappingSlotAsync(string providerId, DateTime startDateTime, DateTime endDateTime, Guid? excludeBookingId = null);
        
        /// <summary>
        /// Get overlapping slots for a provider within a date range
        /// </summary>
        Task<List<ProviderTimeSlot>> GetOverlappingSlotsAsync(string providerId, DateTime startDateTime, DateTime endDateTime);
        
        /// <summary>
        /// Create a new time slot
        /// </summary>
        Task<ProviderTimeSlot> CreateAsync(ProviderTimeSlot timeSlot);
        
        /// <summary>
        /// Update an existing time slot
        /// </summary>
        Task<bool> UpdateAsync(ProviderTimeSlot timeSlot);
        
        /// <summary>
        /// Deactivate a time slot (when booking is cancelled or completed)
        /// </summary>
        Task<bool> DeactivateAsync(Guid timeSlotId);
        
        /// <summary>
        /// Deactivate time slot by booking ID
        /// </summary>
        Task<bool> DeactivateByBookingIdAsync(Guid bookingId);
    }
}
