using System.ComponentModel.DataAnnotations;
using LocalScout.Domain.Enums;

namespace LocalScout.Domain.Entities
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; }
        
        public Guid ServiceId { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public string ProviderId { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        /// <summary>
        /// JSON array of image paths uploaded by the user
        /// </summary>
        public string? ImagePaths { get; set; }
        
        /// <summary>
        /// User's address/area for the job
        /// </summary>
        public string? AddressArea { get; set; }
        
        /// <summary>
        /// The negotiated price entered by provider after acceptance
        /// </summary>
        public decimal? NegotiatedPrice { get; set; }
        
        /// <summary>
        /// Current status of the booking
        /// </summary>
        public BookingStatus Status { get; set; } = BookingStatus.PendingProviderReview;
        
        /// <summary>
        /// Optional notes from provider when accepting/entering price
        /// </summary>
        public string? ProviderNotes { get; set; }
        
        /// <summary>
        /// Reason for cancellation if cancelled
        /// </summary>
        public string? CancellationReason { get; set; }
        
        /// <summary>
        /// Who cancelled the booking (User/Provider/Admin)
        /// </summary>
        public string? CancelledBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? AcceptedAt { get; set; }
        
        public DateTime? PaymentReceivedAt { get; set; }
        
        public DateTime? JobDoneAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? CancelledAt { get; set; }
    }
}