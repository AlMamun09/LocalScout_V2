using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LocalScout.Application.DTOs
{
    public class VerificationSubmissionDto
    {
        [Required(ErrorMessage = "Please select a document type.")]
        public string? DocumentType { get; set; }

        [Required(ErrorMessage = "Please upload a document.")]
        public IFormFile? Document { get; set; }
    }
}
