namespace LocalScout.Application.DTOs.PaymentDTOs
{
    public class PaymentHistoryDto
    {
        public Guid BookingId { get; set; }
        public Guid ServiceId { get; set; }
        public string? TransactionId { get; set; }
        public string? ValidationId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Display properties
        public string ServiceName { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty; // Provider Name for User, User Name for Provider
        public string? OtherPartyImage { get; set; } // Provider/User Profile Picture
        
        public string FormattedDate => PaymentDate.ToString("MMM dd, yyyy hh:mm tt");
        public string FormattedAmount => $"৳{Amount:N2}";
    }
}
