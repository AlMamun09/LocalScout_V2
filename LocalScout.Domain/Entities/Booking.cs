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
        public string? ImagePaths { get; set; }
        public string? AddressArea { get; set; }
        public decimal? NegotiatedPrice { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.PendingProviderReview;
        public string? ProviderNotes { get; set; }
        public string? CancellationReason { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public DateTime? PaymentReceivedAt { get; set; }
        public DateTime? JobDoneAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public bool HasReview { get; set; }

        // Scheduling Properties
        public DateTime? RequestedDate { get; set; }           // Date requested by user
        public TimeSpan? RequestedStartTime { get; set; }      // Start time requested by user
        public TimeSpan? RequestedEndTime { get; set; }        // End time requested by user
        public DateTime? ConfirmedStartDateTime { get; set; }  // Final confirmed start (date+time)
        public DateTime? ConfirmedEndDateTime { get; set; }    // Final confirmed end (date+time)
        public int AutoCancelWarningCount { get; set; } = 0;   // Track auto-cancel count for service blocking

        //Payment Details
        public string? TransactionId { get; set; }
        public string? ValidationId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? BankTransactionId { get; set; }
        public string? PaymentStatus { get; set; }

    }
}