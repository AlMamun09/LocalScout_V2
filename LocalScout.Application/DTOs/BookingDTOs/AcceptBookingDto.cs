namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for provider accepting and setting price
    /// </summary>
    public class AcceptBookingDto
    {
        public Guid BookingId { get; set; }
        public decimal NegotiatedPrice { get; set; }
        public string? ProviderNotes { get; set; }
    }
}
