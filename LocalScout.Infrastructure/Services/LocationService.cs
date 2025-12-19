using LocalScout.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LocalScout.Infrastructure.Services
{
    public class LocationService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://us1.locationiq.com/v1";

        public LocationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["LocationIQ:ApiKey"];
        }

        public async Task<AddressResult?> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                var url =
                    $"{BaseUrl}/reverse.php?key={_apiKey}&lat={latitude}&lon={longitude}&format=json";
                var response = await _httpClient.GetFromJsonAsync<LocationIQReverseResponse>(url);

                if (response == null)
                    return null;

                return new AddressResult
                {
                    DisplayName = response.DisplayName,
                    Latitude = latitude,
                    Longitude = longitude,
                    City =
                        response.Address?.City
                        ?? response.Address?.Town
                        ?? response.Address?.Village,
                    State = response.Address?.State,
                    Country = response.Address?.Country,
                    PostalCode = response.Address?.Postcode,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reverse geocoding error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<AddressSuggestion>> SearchAddressAsync(string query)
        {
            try
            {
                var url =
                    $"{BaseUrl}/search.php?key={_apiKey}&q={Uri.EscapeDataString(query)}&format=json&limit=5";
                var response = await _httpClient.GetFromJsonAsync<List<LocationIQSearchResponse>>(
                    url
                );

                if (response == null)
                    return new List<AddressSuggestion>();

                return response
                    .Select(r => new AddressSuggestion
                    {
                        DisplayName = r.DisplayName,
                        Latitude = double.Parse(r.Lat),
                        Longitude = double.Parse(r.Lon),
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Address search error: {ex.Message}");
                return new List<AddressSuggestion>();
            }
        }

        // Internal response models for LocationIQ API
        private class LocationIQReverseResponse
        {
            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("address")]
            public AddressDetails? Address { get; set; }
        }

        private class AddressDetails
        {
            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("town")]
            public string? Town { get; set; }

            [JsonPropertyName("village")]
            public string? Village { get; set; }

            [JsonPropertyName("state")]
            public string? State { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("postcode")]
            public string? Postcode { get; set; }
        }

        private class LocationIQSearchResponse
        {
            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("lat")]
            public string Lat { get; set; } = string.Empty;

            [JsonPropertyName("lon")]
            public string Lon { get; set; } = string.Empty;
        }
    }
}
