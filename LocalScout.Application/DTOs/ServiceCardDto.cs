namespace LocalScout.Application.DTOs
{
    public class ServiceCardDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string PricingUnit { get; set; } = "Fixed";
        public string? FirstImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Provider Information
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderLocation { get; set; }
        public DateTime ProviderJoinedDate { get; set; }
        public string? WorkingDays { get; set; }
        public string? WorkingHours { get; set; }
        public double? Rating { get; set; }
    }
}
