using System.ComponentModel.DataAnnotations;

namespace LocalScout.Domain.Entities
{
    /// <summary>
    /// Represents a temporarily blocked service due to repeated auto-cancellations.
    /// When a provider fails to respond to 3 consecutive booking requests, the service is blocked for 2 days.
    /// </summary>
    public class ServiceBlock
    {
        [Key]
        public Guid ServiceBlockId { get; set; }
        
        /// <summary>
        /// The service that is blocked
        /// </summary>
        public Guid ServiceId { get; set; }
        
        /// <summary>
        /// Reason for blocking (e.g., "3 consecutive auto-cancellations")
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// When the block was applied (Local timezone - UTC+6)
        /// </summary>
        public DateTime BlockedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// When the block will be automatically lifted (Local timezone - UTC+6)
        /// </summary>
        public DateTime UnblockAt { get; set; }
        
        /// <summary>
        /// Whether this block is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
