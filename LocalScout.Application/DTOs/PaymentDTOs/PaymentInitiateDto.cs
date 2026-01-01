namespace LocalScout.Application.DTOs.PaymentDTOs
{
    /// <summary>
    /// DTO for initiating a payment session with SSLCommerz
    /// </summary>
    public class PaymentInitiateDto
    {
        public Guid BookingId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BDT";
        
        // Customer Information
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerCity { get; set; } = "Dhaka";
        public string CustomerCountry { get; set; } = "Bangladesh";
        
        // Product Information
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = "Service";
        
        // Callback URLs
        public string SuccessUrl { get; set; } = string.Empty;
        public string FailUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string IpnUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from SSLCommerz session initiation
    /// </summary>
    public class SSLCommerzInitResponse
    {
        public bool Success { get; set; }
        public string? GatewayPageUrl { get; set; }
        public string? SessionKey { get; set; }
        public string? FailedReason { get; set; }
    }
}
