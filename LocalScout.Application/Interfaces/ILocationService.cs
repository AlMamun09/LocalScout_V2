namespace LocalScout.Application.Interfaces
{
    public interface ILocationService
    {
        Task<AddressResult?> ReverseGeocodeAsync(double latitude, double longitude);
        Task<List<AddressSuggestion>> SearchAddressAsync(string query);
    }

    public class AddressResult
    {
        public string DisplayName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }

    public class AddressSuggestion
    {
        public string DisplayName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
