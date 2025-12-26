using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// Detailed booking DTO for provider view - includes user contact info immediately
    /// </summary>
    public class BookingDetailsForProviderDto
    {
        public Guid BookingId { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public decimal ServiceMinPrice { get; set; }
        public string? ServicePricingUnit { get; set; }

        // User info - ALWAYS visible to provider once booking is created
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
        public string? UserProfilePicture { get; set; }
        public string? UserAddress { get; set; }

        // Booking details
        public string? Description { get; set; }
        public List<string> ImagePaths { get; set; } = new();
        public string? AddressArea { get; set; }
        public decimal? NegotiatedPrice { get; set; }
        public string? ProviderNotes { get; set; }

        // Status
        public BookingStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? PaymentReceivedAt { get; set; }
        public DateTime? JobDoneAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Actions available
        public bool CanAcceptAndSetPrice => Status == BookingStatus.PendingProviderReview;
        public bool CanMarkJobDone => Status == BookingStatus.PaymentReceived || Status == BookingStatus.InProgress;
        public bool CanCancel => Status == BookingStatus.PendingProviderReview ||
                                  Status == BookingStatus.AcceptedByProvider;
    }
}
