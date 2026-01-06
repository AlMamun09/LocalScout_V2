using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for creating a reschedule proposal
    /// </summary>
    public class CreateRescheduleProposalDto
    {
        public Guid BookingId { get; set; }
        public DateTime ProposedDate { get; set; }
        public TimeSpan ProposedStartTime { get; set; }
        public TimeSpan ProposedEndTime { get; set; }
        public string? Message { get; set; }
    }
    
    /// <summary>
    /// DTO for displaying a reschedule proposal
    /// </summary>
    public class RescheduleProposalDto
    {
        public Guid ProposalId { get; set; }
        public Guid BookingId { get; set; }
        public string ProposedBy { get; set; } = string.Empty;
        public string ProposedByName { get; set; } = string.Empty;
        public DateTime ProposedDate { get; set; }
        public TimeSpan ProposedStartTime { get; set; }
        public TimeSpan ProposedEndTime { get; set; }
        public string? Message { get; set; }
        public RescheduleStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public bool CanRespond { get; set; }
    }
    
    /// <summary>
    /// DTO for responding to a reschedule proposal
    /// </summary>
    public class RespondRescheduleDto
    {
        public Guid ProposalId { get; set; }
        public bool Accept { get; set; }
        public string? ResponseMessage { get; set; }
    }
}
