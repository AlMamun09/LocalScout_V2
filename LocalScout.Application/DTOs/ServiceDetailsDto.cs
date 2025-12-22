namespace LocalScout.Application.DTOs
{
    public class ServiceDetailsDto
    {
        // Service Information
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MinPrice { get; set; }
        public string PricingUnit { get; set; } = "Fixed";
        public List<string> ImagePaths { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        // Category Information
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        // Provider Information
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderBusinessName { get; set; }
        public string? ProviderProfilePicture { get; set; }
        public string? ProviderDescription { get; set; }
        public string? ProviderLocation { get; set; }
        public double? ProviderLatitude { get; set; }
        public double? ProviderLongitude { get; set; }
        public string? ProviderPhone { get; set; }
        public string? WorkingDays { get; set; }
        public string? WorkingHours { get; set; }
        public DateTime ProviderJoinedDate { get; set; }
        public bool IsProviderVerified { get; set; }

        // Rating (placeholder for now)
        public double Rating { get; set; } = 4.6;
        public int ReviewCount { get; set; } = 0;

        // Distance (in kilometers)
        public double? DistanceInKm { get; set; }

        // Related Services
        public List<ServiceCardDto> OtherProviderServices { get; set; } = new();
        public List<ServiceCardDto> RelatedServices { get; set; } = new();

        // Price display helper
        public string GetPriceDisplay()
        {
            var unit = PricingUnit switch
            {
                "Per Hour" => "/Hour",
                "Per Day" => "/Day",
                "Per Project" => "",
                _ => ""
            };

            if (PricingUnit == "Fixed")
            {
                return $"{MinPrice:N0} Tk";
            }

            return $"From {MinPrice:N0} Tk{unit}";
        }

        // Distance display helper
        public string GetDistanceDisplay()
        {
            if (!DistanceInKm.HasValue)
                return string.Empty;

            if (DistanceInKm.Value < 1)
                return $"{(DistanceInKm.Value * 1000):N0}m away";

            return $"{DistanceInKm.Value:N1}km away";
        }
    }
}
