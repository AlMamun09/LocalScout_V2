using System.ComponentModel.DataAnnotations;

namespace LocalScout.Domain.Entities
{
    public class Service
    {
        [Key]
        public Guid ServiceId { get; set; }
        public string? Id { get; set; }
        public Guid ServiceCategoryId { get; set; }
        public string? ServiceName { get; set; }
        public string? Description { get; set; }
        public string? PricingUnit { get; set; }
        public decimal Price { get; set; }
        public string? ImagePaths { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
