using LocalScout.Domain.Enums;
using LocalScout.Application.Extensions;

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

        // Time Schedule - Requested by user
        public DateTime? RequestedDate { get; set; }
        public TimeSpan? RequestedStartTime { get; set; }
        public TimeSpan? RequestedEndTime { get; set; }
        
        // Time Schedule - Confirmed by provider
        public DateTime? ConfirmedStartDateTime { get; set; }
        public DateTime? ConfirmedEndDateTime { get; set; }
        
        // Formatted time display helpers
        public string? RequestedScheduleFormatted => RequestedDate.HasValue && RequestedStartTime.HasValue && RequestedEndTime.HasValue
            ? $"{RequestedDate.Value:MMM dd, yyyy} • {DateTime.Today.Add(RequestedStartTime.Value):h:mm tt} - {DateTime.Today.Add(RequestedEndTime.Value):h:mm tt}"
            : null;
            
        public string? ConfirmedScheduleFormatted => ConfirmedStartDateTime.HasValue && ConfirmedEndDateTime.HasValue
            ? $"{ConfirmedStartDateTime.Value:MMM dd, yyyy} • {ConfirmedStartDateTime.Value:h:mm tt} - {ConfirmedEndDateTime.Value:h:mm tt}"
            : null;

        // Status
        public BookingStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => CreatedAt.ToBdTimeString("MMM dd, yyyy h:mm tt");
        public DateTime? AcceptedAt { get; set; }
        public string? AcceptedAtFormatted => AcceptedAt.ToBdTimeString("MMM dd, yyyy h:mm tt");

        // Actions
        public bool CanAcceptAndSetPrice { get; set; }
        public bool CanMarkJobDone { get; set; }
        public bool CanPay { get; set; }
        public bool CanCancel { get; set; }
        public bool CanConfirmCompletion { get; set; }
    }
}

