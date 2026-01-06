using System.ComponentModel.DataAnnotations;

namespace LocalScout.Domain.Entities
{
    /// <summary>
    /// Represents a locked time slot for a provider after accepting a booking.
    /// Used to track provider availability and prevent overlapping bookings.
    /// </summary>
    public class ProviderTimeSlot
    {
        [Key]
        public Guid TimeSlotId { get; set; }
        
        /// <summary>
        /// The provider's user ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;
        
        /// <summary>
        /// The booking ID that created this time slot
        /// </summary>
        public Guid BookingId { get; set; }
        
        /// <summary>
        /// Start date and time of the locked slot (Local timezone - UTC+6)
        /// </summary>
        public DateTime StartDateTime { get; set; }
        
        /// <summary>
        /// End date and time of the locked slot (Local timezone - UTC+6)
        /// </summary>
        public DateTime EndDateTime { get; set; }
        
        /// <summary>
        /// Whether this time slot is currently active/valid
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
