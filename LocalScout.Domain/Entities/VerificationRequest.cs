using LocalScout.Domain.Enums;

namespace LocalScout.Domain.Entities
{
    public class VerificationRequest
    {
        public Guid VerificationRequestId { get; set; }
        public string? ProviderId { get; set; }
        public string? DocumentPath { get; set; }
        public string? DocumentType { get; set; }
        public VerificationStatus Status { get; set; }
        public string? AdminComments { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
