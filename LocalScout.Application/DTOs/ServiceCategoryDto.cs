using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LocalScout.Application.DTOs
{
    public class ServiceCategoryDto
    {
        public Guid ServiceCategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100, ErrorMessage = "Category name must be 100 characters or less")]
        [Display(Name = "Category Name")]
        public string? CategoryName { get; set; }

        [StringLength(500, ErrorMessage = "Description must be 500 characters or less")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string? IconPath { get; set; }

        [Display(Name = "Category Icon (Image)")]
        public IFormFile? IconFile { get; set; }
    }
}
