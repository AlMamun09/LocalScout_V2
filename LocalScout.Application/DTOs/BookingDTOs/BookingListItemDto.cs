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
        public string? RequestedScheduleFormatted
        {
            get
            {
                if (!RequestedDate.HasValue || !RequestedStartTime.HasValue)
                    return null;
                
                var dateStr = RequestedDate.Value.ToString("MMM dd, yyyy");
                var startTimeStr = DateTime.Today.Add(RequestedStartTime.Value).ToString("h:mm tt");
                
                // If end time is provided, show range; otherwise just show preferred time
                if (RequestedEndTime.HasValue && RequestedEndTime.Value != RequestedStartTime.Value)
                {
                    var endTimeStr = DateTime.Today.Add(RequestedEndTime.Value).ToString("h:mm tt");
                    return $"{dateStr} • {startTimeStr} - {endTimeStr}";
                }
                
                return $"{dateStr} • {startTimeStr}";
            }
        }
        
        // Formatted preferred time (start time only) for display
        public string? PreferredTimeFormatted
        {
            get
            {
                if (!RequestedDate.HasValue || !RequestedStartTime.HasValue)
                    return null;
                
                var dateStr = RequestedDate.Value.ToString("MMM dd, yyyy");
                var startTimeStr = DateTime.Today.Add(RequestedStartTime.Value).ToString("h:mm tt");
                return $"{dateStr} at {startTimeStr}";
            }
        }
        
        // Check if user provided end time
        public bool HasRequestedEndTime => RequestedEndTime.HasValue && RequestedEndTime.Value != RequestedStartTime;
            
        public string? ConfirmedScheduleFormatted
        {
            get
            {
                if (!ConfirmedStartDateTime.HasValue || !ConfirmedEndDateTime.HasValue)
                    return null;

                if (ConfirmedStartDateTime.Value.Date == ConfirmedEndDateTime.Value.Date)
                {
                    // Same day: "Jan 23, 2026 • 9:00 AM - 5:00 PM"
                    return $"{ConfirmedStartDateTime.Value:MMM dd, yyyy} • {ConfirmedStartDateTime.Value:h:mm tt} - {ConfirmedEndDateTime.Value:h:mm tt}";
                }
                else
                {
                    // Different days: "Jan 23, 2026 • 9:00 AM - Jan 25, 2026 • 5:00 PM"
                    return $"{ConfirmedStartDateTime.Value:MMM dd, yyyy} • {ConfirmedStartDateTime.Value:h:mm tt} - {ConfirmedEndDateTime.Value:MMM dd, yyyy} • {ConfirmedEndDateTime.Value:h:mm tt}";
                }
            }
        }

        // Proposed Schedule (for PendingUserApproval/PendingProviderApproval)
        public DateTime? ProposedStartDateTime { get; set; }
        public DateTime? ProposedEndDateTime { get; set; }
        public decimal? ProposedPrice { get; set; }
        public string? ProposedNotes { get; set; }
        public string? ProposedBy { get; set; }
        
        public string? ProposedScheduleFormatted
        {
            get
            {
                if (!ProposedStartDateTime.HasValue)
                    return null;

                if (!ProposedEndDateTime.HasValue)
                    return $"{ProposedStartDateTime.Value:MMM dd, yyyy h:mm tt}";

                if (ProposedStartDateTime.Value.Date == ProposedEndDateTime.Value.Date)
                {
                    return $"{ProposedStartDateTime.Value:MMM dd, yyyy} • {ProposedStartDateTime.Value:h:mm tt} - {ProposedEndDateTime.Value:h:mm tt}";
                }
                else
                {
                    return $"{ProposedStartDateTime.Value:MMM dd, yyyy} • {ProposedStartDateTime.Value:h:mm tt} - {ProposedEndDateTime.Value:MMM dd, yyyy} • {ProposedEndDateTime.Value:h:mm tt}";
                }
            }
        }
        
        public bool HasProposal => ProposedStartDateTime.HasValue;

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
        public bool CanStartJob { get; set; }
        public bool CanComplete { get; set; }
        public bool CanMarkJobDone { get; set; }
        public bool CanPay { get; set; }
        public bool CanCancel { get; set; }
        public bool CanConfirmCompletion { get; set; }
    }
}

