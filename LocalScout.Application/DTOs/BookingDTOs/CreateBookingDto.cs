using System.ComponentModel.DataAnnotations;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for creating a new booking request
    /// </summary>
    public class CreateBookingDto
    {
        [Required(ErrorMessage = "Service is required")]
        public Guid ServiceId { get; set; }

        [StringLength(1000, ErrorMessage = "Description must be 1000 characters or less")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Address must be 500 characters or less")]
        public string? AddressArea { get; set; }

        public List<string>? ImagePaths { get; set; }
        
        // Scheduling fields
        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime RequestedDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan RequestedStartTime { get; set; }

        public TimeSpan? RequestedEndTime { get; set; } // Optional - provider will set this
    }
}
