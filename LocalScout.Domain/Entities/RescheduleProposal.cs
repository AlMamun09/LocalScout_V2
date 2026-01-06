using System.ComponentModel.DataAnnotations;
using LocalScout.Domain.Enums;

namespace LocalScout.Domain.Entities
{
    public class RescheduleProposal
    {
        [Key]
        public Guid ProposalId { get; set; }
        public Guid BookingId { get; set; }
        public string ProposedBy { get; set; } = string.Empty;
        public string ProposedByUserId { get; set; } = string.Empty;
        public DateTime ProposedDate { get; set; }
        public TimeSpan ProposedStartTime { get; set; }
        public TimeSpan ProposedEndTime { get; set; }
        public string? Message { get; set; }
        public RescheduleStatus Status { get; set; } = RescheduleStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? RespondedAt { get; set; }
    }
}
