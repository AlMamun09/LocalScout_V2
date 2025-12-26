namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// DTO for create booking modal (replaces ViewModel in controller)
    /// </summary>
    public class CreateBookingModalDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServiceMinPrice { get; set; }
        public string? ServicePricingUnit { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderBusinessName { get; set; }
        public string? UserAddress { get; set; }
    }
}
