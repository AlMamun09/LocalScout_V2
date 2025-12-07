using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LocalScout.Application.DTOs
{
    public class ServiceCategoryDto
    {
        public Guid ServiceCategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [Display(Name = "Category Name")]
        public string? CategoryName { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string? IconPath { get; set; }

        [Display(Name = "Category Icon (Image)")]
        public IFormFile? IconFile { get; set; }
    }
}
