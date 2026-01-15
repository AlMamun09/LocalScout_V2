using LocalScout.Application.DTOs;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;

namespace LocalScout.Application.Interfaces
{
    public interface IBookingRepository
    {
        // Basic CRUD
        Task<Booking?> GetByIdAsync(Guid bookingId);
        Task<Booking> CreateAsync(Booking booking);
        Task<Booking> UpdateAsync(Booking booking);
        
        // All bookings (for admin)
        Task<List<Booking>> GetAllBookingsAsync();
        
        // User queries
        Task<List<Booking>> GetUserBookingsAsync(string userId, BookingStatus? status = null);
        Task<int> GetUserBookingCountAsync(string userId, BookingStatus? status = null);
        Task<int> GetUserActiveBookingCountAsync(string userId);
        Task<int> GetUserCompletedBookingCountAsync(string userId);
        Task<decimal> GetUserTotalSpentAsync(string userId);
        
        // Provider queries
        Task<List<Booking>> GetProviderBookingsAsync(string providerId, BookingStatus? status = null);
        Task<int> GetProviderBookingCountAsync(string providerId, BookingStatus? status = null);
        Task<int> GetProviderPendingRequestCountAsync(string providerId);
        Task<int> GetProviderCompletedBookingCountAsync(string providerId);
        Task<decimal> GetProviderTotalEarningsAsync(string providerId);
        
        // Status updates
        Task<bool> AcceptBookingAsync(Guid bookingId, decimal negotiatedPrice, string? providerNotes);
        
        /// <summary>
        /// Accept booking with confirmed time slot
        /// </summary>
        Task<bool> AcceptBookingAsync(Guid bookingId, decimal negotiatedPrice, string? providerNotes, 
            DateTime confirmedStartDateTime, DateTime confirmedEndDateTime);
        
        Task<bool> UpdateStatusAsync(Guid bookingId, BookingStatus newStatus);
        Task<bool> CancelBookingAsync(Guid bookingId, string cancelledBy, string? reason);
        Task<bool> MarkPaymentReceivedAsync(Guid bookingId);
        Task<bool> MarkPaymentReceivedAsync(Guid bookingId, string transactionId, string validationId, string paymentMethod, string? bankTxnId);
        Task<Booking?> GetByTransactionIdAsync(string transactionId);
        Task<bool> SetTransactionIdAsync(Guid bookingId, string transactionId);
        Task<bool> MarkJobDoneAsync(Guid bookingId);
        Task<bool> MarkCompletedAsync(Guid bookingId);
        
        // Validation
        Task<bool> IsBookingOwnerAsync(Guid bookingId, string userId);
        Task<bool> IsBookingProviderAsync(Guid bookingId, string providerId);
        
        /// <summary>
        /// Check if user has a booking in JobDone status awaiting completion confirmation
        /// </summary>
        Task<bool> HasPendingCompletionAsync(string userId);
        
        // Payment History
        Task<List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>> GetPaymentHistoryForUserAsync(string userId);
        Task<List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>> GetPaymentHistoryForProviderAsync(string providerId);
        
        // Scheduling-related methods
        /// <summary>
        /// Check if user has an active (pending/accepted) booking for a specific service
        /// </summary>
        Task<bool> HasActiveBookingForServiceAsync(string userId, Guid serviceId);
        
        /// <summary>
        /// Get pending bookings that have exceeded the timeout period (12 hours)
        /// </summary>
        Task<List<Booking>> GetExpiredPendingBookingsAsync(TimeSpan timeout);
        
        /// <summary>
        /// Get count of auto-cancelled bookings for a service (for blocking logic)
        /// </summary>
        Task<int> GetAutoCancelCountForServiceAsync(Guid serviceId, TimeSpan withinPeriod);
        
        /// <summary>
        /// Update booking time (for provider adjustments)
        /// </summary>
        Task<bool> UpdateBookingTimeAsync(Guid bookingId, DateTime newStartDateTime, DateTime newEndDateTime);
        
        #region Limit Checking Methods
        
        /// <summary>
        /// Get count of pending requests for a specific service
        /// </summary>
        Task<int> GetPendingRequestCountForServiceAsync(Guid serviceId);
        
        /// <summary>
        /// Get count of provider's currently accepted bookings (AcceptedByProvider, PaymentReceived, InProgress)
        /// </summary>
        Task<int> GetProviderAcceptedBookingCountAsync(string providerId);
        
        /// <summary>
        /// Get count of user's cancellations in the current month
        /// </summary>
        Task<int> GetUserMonthlyCancellationCountAsync(string userId);
        
        /// <summary>
        /// Get count of user's active bookings with a specific provider
        /// </summary>
        Task<int> GetUserActiveBookingsWithProviderAsync(string userId, string providerId);
        
        /// <summary>
        /// Get count of user's total pending requests (awaiting provider response)
        /// </summary>
        Task<int> GetUserPendingRequestCountAsync(string userId);
        
        #endregion
    }
}

