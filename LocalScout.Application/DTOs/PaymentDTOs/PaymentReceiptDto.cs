namespace LocalScout.Application.DTOs.PaymentDTOs
{
    /// <summary>
    /// DTO for generating payment receipt PDF
    /// </summary>
    public class PaymentReceiptDto
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public Guid BookingId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? ValidationId { get; set; }
        public string? BankTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        
        // Service info
        public string ServiceName { get; set; } = string.Empty;
        
        // Provider info
        public string ProviderName { get; set; } = string.Empty;
        public string? ProviderPhone { get; set; }
        public string? ProviderEmail { get; set; }
        
        // Customer info
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
    }
}
