namespace LocalScout.Application.DTOs.PaymentDTOs
{
    /// <summary>
    /// Configuration settings for SSLCommerz payment gateway
    /// </summary>
    public class SSLCommerzSettings
    {
        public string StoreId { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public bool IsSandbox { get; set; } = true;
        public string SandboxUrl { get; set; } = "https://sandbox.sslcommerz.com/gwprocess/v3/api.php";
        public string LiveUrl { get; set; } = "https://securepay.sslcommerz.com/gwprocess/v3/api.php";
        public string SandboxValidationUrl { get; set; } = "https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php";
        public string LiveValidationUrl { get; set; } = "https://securepay.sslcommerz.com/validator/api/validationserverAPI.php";

        public string ApiUrl => IsSandbox ? SandboxUrl : LiveUrl;
        public string ValidationUrl => IsSandbox ? SandboxValidationUrl : LiveValidationUrl;
    }
}
