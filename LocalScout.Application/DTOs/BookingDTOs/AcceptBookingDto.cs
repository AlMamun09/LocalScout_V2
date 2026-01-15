using System.ComponentModel.DataAnnotations;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider accepting and setting price with confirmed time
    /// </summary>
    public class AcceptBookingDto
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be at least 1")]
        public decimal NegotiatedPrice { get; set; }

        [StringLength(500)]
        public string? ProviderNotes { get; set; }
        
        // Confirmed scheduling (provider confirms/adjusts the time)
        [Required]
        public DateTime ConfirmedDate { get; set; }

        [Required]
        public DateTime ConfirmedEndDate { get; set; }  // For multi-day bookings

        [Required]
        public TimeSpan ConfirmedStartTime { get; set; }

        [Required]
        public TimeSpan ConfirmedEndTime { get; set; }

        /// <summary>
        /// When true, provider has confirmed acceptance even though the booking
        /// extends beyond their configured working hours
        /// </summary>
        public bool ConfirmOutsideWorkingHours { get; set; }
    }
}
