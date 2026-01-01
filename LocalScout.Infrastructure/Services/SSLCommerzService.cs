using LocalScout.Application.DTOs.PaymentDTOs;
using LocalScout.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// SSLCommerz payment gateway service implementation
    /// </summary>
    public class SSLCommerzService : ISSLCommerzService
    {
        private readonly SSLCommerzSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SSLCommerzService> _logger;

        public SSLCommerzService(
            IOptions<SSLCommerzSettings> settings,
            HttpClient httpClient,
            ILogger<SSLCommerzService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GenerateTransactionId(Guid bookingId)
        {
            // Format: LS + first 8 chars of booking GUID + timestamp (max 25 chars)
            var bookingPart = bookingId.ToString("N").Substring(0, 8).ToUpper();
            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            return $"LS{bookingPart}{timestamp}";
        }

        public async Task<SSLCommerzInitResponse> InitiatePaymentAsync(PaymentInitiateDto dto)
        {
            try
            {
                var formData = new Dictionary<string, string>
                {
                    // Store credentials
                    { "store_id", _settings.StoreId },
                    { "store_passwd", _settings.StorePassword },

                    // Transaction info
                    { "total_amount", dto.Amount.ToString("F2") },
                    { "currency", dto.Currency },
                    { "tran_id", dto.TransactionId },

                    // URLs
                    { "success_url", dto.SuccessUrl },
                    { "fail_url", dto.FailUrl },
                    { "cancel_url", dto.CancelUrl },
                    { "ipn_url", dto.IpnUrl },

                    // Customer info
                    { "cus_name", dto.CustomerName },
                    { "cus_email", dto.CustomerEmail },
                    { "cus_phone", dto.CustomerPhone },
                    { "cus_add1", dto.CustomerAddress },
                    { "cus_city", dto.CustomerCity },
                    { "cus_country", dto.CustomerCountry },

                    // Product info
                    { "product_name", dto.ProductName },
                    { "product_category", dto.ProductCategory },
                    { "product_profile", "general" },

                    // Shipping info (required but can be same as customer)
                    { "shipping_method", "NO" },
                    { "num_of_item", "1" },

                    // Value added parameters
                    { "value_a", dto.BookingId.ToString() }
                };

                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("SSLCommerz Init Response: {Response}", responseContent);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (jsonResponse.TryGetProperty("status", out var statusElement) &&
                    statusElement.GetString() == "SUCCESS")
                {
                    return new SSLCommerzInitResponse
                    {
                        Success = true,
                        GatewayPageUrl = jsonResponse.GetProperty("GatewayPageURL").GetString(),
                        SessionKey = jsonResponse.TryGetProperty("sessionkey", out var sk) ? sk.GetString() : null
                    };
                }
                else
                {
                    var failedReason = jsonResponse.TryGetProperty("failedreason", out var fr) 
                        ? fr.GetString() 
                        : "Unknown error";
                    
                    _logger.LogWarning("SSLCommerz session failed: {Reason}", failedReason);
                    
                    return new SSLCommerzInitResponse
                    {
                        Success = false,
                        FailedReason = failedReason
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating SSLCommerz payment");
                return new SSLCommerzInitResponse
                {
                    Success = false,
                    FailedReason = ex.Message
                };
            }
        }

        public async Task<SSLCommerzValidationResponse?> ValidateTransactionAsync(string validationId)
        {
            try
            {
                var url = $"{_settings.ValidationUrl}?val_id={validationId}&store_id={_settings.StoreId}&store_passwd={_settings.StorePassword}&format=json";

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("SSLCommerz Validation Response: {Response}", responseContent);

                var validationResponse = JsonSerializer.Deserialize<SSLCommerzValidationResponse>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return validationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SSLCommerz transaction");
                return null;
            }
        }
    }
}
