using LocalScout.Application.DTOs.PaymentDTOs;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Interface for SSLCommerz payment gateway operations
    /// </summary>
    public interface ISSLCommerzService
    {
        /// <summary>
        /// Initiates a payment session with SSLCommerz
        /// </summary>
        /// <param name="dto">Payment initiation details</param>
        /// <returns>Response containing gateway URL for redirect</returns>
        Task<SSLCommerzInitResponse> InitiatePaymentAsync(PaymentInitiateDto dto);

        /// <summary>
        /// Validates a completed transaction using SSLCommerz validation API
        /// </summary>
        /// <param name="validationId">The val_id returned by SSLCommerz</param>
        /// <returns>Validation response with transaction details</returns>
        Task<SSLCommerzValidationResponse?> ValidateTransactionAsync(string validationId);

        /// <summary>
        /// Generates a unique transaction ID for a booking
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <returns>Unique transaction ID (max 25 chars)</returns>
        string GenerateTransactionId(Guid bookingId);
    }
}
