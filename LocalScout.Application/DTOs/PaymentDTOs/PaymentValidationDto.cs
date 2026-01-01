namespace LocalScout.Application.DTOs.PaymentDTOs
{
    /// <summary>
    /// DTO for SSLCommerz callback/IPN response data
    /// Maps the form data posted by SSLCommerz on success/fail/cancel
    /// </summary>
    public class SSLCommerzCallbackDto
    {
        // Transaction identifiers
        public string? tran_id { get; set; }
        public string? val_id { get; set; }
        public string? bank_tran_id { get; set; }
        
        // Amount info
        public string? amount { get; set; }
        public string? store_amount { get; set; }
        public string? currency { get; set; }
        
        // Status
        public string? status { get; set; }
        
        // Payment method
        public string? card_type { get; set; }
        public string? card_no { get; set; }
        public string? card_issuer { get; set; }
        public string? card_brand { get; set; }
        public string? card_issuer_country { get; set; }
        
        // Verification
        public string? verify_sign { get; set; }
        public string? verify_key { get; set; }
        
        // Error info
        public string? error { get; set; }
        
        // Timestamps
        public string? tran_date { get; set; }
        
        // Risk assessment
        public string? risk_level { get; set; }
        public string? risk_title { get; set; }
    }

    /// <summary>
    /// Response from SSLCommerz transaction validation API
    /// </summary>
    public class SSLCommerzValidationResponse
    {
        public string? status { get; set; }
        public string? tran_date { get; set; }
        public string? tran_id { get; set; }
        public string? val_id { get; set; }
        public string? amount { get; set; }
        public string? store_amount { get; set; }
        public string? currency { get; set; }
        public string? bank_tran_id { get; set; }
        public string? card_type { get; set; }
        public string? card_no { get; set; }
        public string? card_issuer { get; set; }
        public string? card_brand { get; set; }
        public string? risk_level { get; set; }
        public string? risk_title { get; set; }
        
        public bool IsValid => status == "VALID" || status == "VALIDATED";
    }

    /// <summary>
    /// DTO for displaying payment result to user
    /// </summary>
    public class PaymentResultDto
    {
        public Guid BookingId { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ServiceName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
}
