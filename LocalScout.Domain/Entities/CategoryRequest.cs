using LocalScout.Domain.Enums;
using System.ComponentModel.DataAnnotations;
namespace LocalScout.Domain.Entities
{
    public class CategoryRequest
    {
        [Key]
        public Guid CategoryRequestId { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string RequestedCategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
        public string? AdminReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}