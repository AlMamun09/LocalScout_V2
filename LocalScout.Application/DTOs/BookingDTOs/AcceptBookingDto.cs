namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider accepting and setting price with confirmed time
    /// </summary>
    public class AcceptBookingDto
    {
        public Guid BookingId { get; set; }
        public decimal NegotiatedPrice { get; set; }
        public string? ProviderNotes { get; set; }
        
        // Confirmed scheduling (provider confirms/adjusts the time)
        public DateTime ConfirmedDate { get; set; }
        public TimeSpan ConfirmedStartTime { get; set; }
        public TimeSpan ConfirmedEndTime { get; set; }
    }
}
