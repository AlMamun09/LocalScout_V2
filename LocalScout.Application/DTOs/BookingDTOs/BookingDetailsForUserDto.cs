using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// Detailed booking DTO for user view - provider contact info only visible after acceptance
    /// </summary>
    public class BookingDetailsForUserDto
    {
        public Guid BookingId { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public decimal ServiceMinPrice { get; set; }
        public string? ServicePricingUnit { get; set; }
        public string? ServiceImagePath { get; set; }

        // Provider basic info - always visible
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderBusinessName { get; set; }
        public string? ProviderProfilePicture { get; set; }
        public bool IsProviderVerified { get; set; }

        // Provider contact info - ONLY visible after AcceptedByProvider status
        public string? ProviderEmail { get; set; }
        public string? ProviderPhone { get; set; }
        public string? ProviderAddress { get; set; }
        public bool CanSeeProviderContact { get; set; }

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
        public bool CanPay => Status == BookingStatus.AcceptedByProvider ||
                              Status == BookingStatus.AwaitingPayment;
        public bool CanCancel => Status == BookingStatus.PendingProviderReview ||
                                  Status == BookingStatus.AcceptedByProvider;
        public bool CanConfirmCompletion => Status == BookingStatus.JobDone;
    }
}
