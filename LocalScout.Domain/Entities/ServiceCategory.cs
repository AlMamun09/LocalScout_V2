using System.ComponentModel.DataAnnotations;

namespace LocalScout.Domain.Entities
{
    public class ServiceCategory
    {
        [Key]
        public Guid ServiceCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public string? IconPath { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
