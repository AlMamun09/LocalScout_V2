using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs
{
    public class CategoryRequestDto
    {
        public Guid CategoryRequestId { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderProfilePictureUrl { get; set; }
        public string RequestedCategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public VerificationStatus Status { get; set; }
        public string? AdminReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
