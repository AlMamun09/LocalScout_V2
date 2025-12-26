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
        Task<bool> UpdateStatusAsync(Guid bookingId, BookingStatus newStatus);
        Task<bool> CancelBookingAsync(Guid bookingId, string cancelledBy, string? reason);
        Task<bool> MarkPaymentReceivedAsync(Guid bookingId);
        Task<bool> MarkJobDoneAsync(Guid bookingId);
        Task<bool> MarkCompletedAsync(Guid bookingId);
        
        // Validation
        Task<bool> IsBookingOwnerAsync(Guid bookingId, string userId);
        Task<bool> IsBookingProviderAsync(Guid bookingId, string providerId);
    }
}
