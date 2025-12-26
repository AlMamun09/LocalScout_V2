using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for booking list item in AJAX responses
    /// </summary>
    public class BookingListItemDto
    {
        public Guid BookingId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceImagePath { get; set; }

        // For user view
        public string? ProviderName { get; set; }
        public string? ProviderBusinessName { get; set; }
        public string? ProviderProfilePicture { get; set; }
        public bool IsProviderVerified { get; set; }
        public string? ProviderPhone { get; set; }
        public string? ProviderEmail { get; set; }
        public string? ProviderAddress { get; set; }
        public bool CanSeeProviderContact { get; set; }

        // For provider view
        public string? UserName { get; set; }
        public string? UserProfilePicture { get; set; }
        public string? UserPhone { get; set; }
        public string? UserEmail { get; set; }
        public string? UserAddress { get; set; }

        // Booking info
        public string? Description { get; set; }
        public string? AddressArea { get; set; }
        public decimal? NegotiatedPrice { get; set; }
        public decimal ServiceMinPrice { get; set; }
        public List<string> ImagePaths { get; set; } = new();

        // Status
        public BookingStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted { get; set; } = string.Empty;
        public DateTime? AcceptedAt { get; set; }
        public string? AcceptedAtFormatted { get; set; }

        // Actions
        public bool CanAcceptAndSetPrice { get; set; }
        public bool CanMarkJobDone { get; set; }
        public bool CanPay { get; set; }
        public bool CanCancel { get; set; }
        public bool CanConfirmCompletion { get; set; }
    }
}
