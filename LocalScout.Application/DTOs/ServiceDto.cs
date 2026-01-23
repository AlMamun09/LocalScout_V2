using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LocalScout.Application.DTOs
{
    public class ServiceDto
    {
        public Guid ServiceId { get; set; }
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public Guid ServiceCategoryId { get; set; }

        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Service name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Service name must be between 2 and 200 characters")]
        public string? ServiceName { get; set; }

        [StringLength(2000, ErrorMessage = "Description must be 2000 characters or less")]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? PricingUnit { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be at least 1")]
        public decimal MinPrice { get; set; }

        public string? ImagePaths { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // For file uploads
        public List<IFormFile>? Images { get; set; }
        public string? ExistingImageUrls { get; set; }

        // Price display helper
        public string GetPriceDisplay()
        {
            var unit = PricingUnit switch
            {
                "Per Hour" => "",
                "Per Day" => "",
                "Per Project" => "",
                _ => ""
            };

            if (PricingUnit == "Fixed")
            {
                return $"{MinPrice:N0} Tk";
            }

            return $"From {MinPrice:N0} Tk{unit}";
        }
    }
}
