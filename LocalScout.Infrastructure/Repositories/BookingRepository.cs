using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
        }

        public async Task<Booking> CreateAsync(Booking booking)
        {
            booking.BookingId = Guid.NewGuid();
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            booking.Status = BookingStatus.PendingProviderReview;
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task<Booking> UpdateAsync(Booking booking)
        {
            booking.UpdatedAt = DateTime.UtcNow;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        // User queries
        public async Task<List<Booking>> GetUserBookingsAsync(string userId, BookingStatus? status = null)
        {
            var query = _context.Bookings.Where(b => b.UserId == userId);
            
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }
            
            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<int> GetUserBookingCountAsync(string userId, BookingStatus? status = null)
        {
            var query = _context.Bookings.Where(b => b.UserId == userId);
            
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }
            
            return await query.CountAsync();
        }

        public async Task<int> GetUserActiveBookingCountAsync(string userId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.UserId == userId && 
                b.Status != BookingStatus.Completed && 
                b.Status != BookingStatus.Cancelled);
        }

        public async Task<int> GetUserCompletedBookingCountAsync(string userId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.UserId == userId && 
                b.Status == BookingStatus.Completed);
        }

        public async Task<decimal> GetUserTotalSpentAsync(string userId)
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId && 
                            b.Status == BookingStatus.Completed && 
                            b.NegotiatedPrice.HasValue)
                .SumAsync(b => b.NegotiatedPrice!.Value);
        }

        // Provider queries
        public async Task<List<Booking>> GetProviderBookingsAsync(string providerId, BookingStatus? status = null)
        {
            var query = _context.Bookings.Where(b => b.ProviderId == providerId);
            
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }
            
            return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        }

        public async Task<int> GetProviderBookingCountAsync(string providerId, BookingStatus? status = null)
        {
            var query = _context.Bookings.Where(b => b.ProviderId == providerId);
            
            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }
            
            return await query.CountAsync();
        }

        public async Task<int> GetProviderPendingRequestCountAsync(string providerId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.ProviderId == providerId && 
                b.Status == BookingStatus.PendingProviderReview);
        }

        public async Task<int> GetProviderCompletedBookingCountAsync(string providerId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.ProviderId == providerId && 
                b.Status == BookingStatus.Completed);
        }

        public async Task<decimal> GetProviderTotalEarningsAsync(string providerId)
        {
            return await _context.Bookings
                .Where(b => b.ProviderId == providerId && 
                            b.Status == BookingStatus.Completed && 
                            b.NegotiatedPrice.HasValue)
                .SumAsync(b => b.NegotiatedPrice!.Value);
        }

        // Status updates
        public async Task<bool> AcceptBookingAsync(Guid bookingId, decimal negotiatedPrice, string? providerNotes)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null || booking.Status != BookingStatus.PendingProviderReview)
            {
                return false;
            }

            booking.NegotiatedPrice = negotiatedPrice;
            booking.ProviderNotes = providerNotes;
            booking.Status = BookingStatus.AcceptedByProvider;
            booking.AcceptedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(Guid bookingId, BookingStatus newStatus)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, string cancelledBy, string? reason)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            // Only allow cancellation in certain states
            if (booking.Status != BookingStatus.PendingProviderReview && 
                booking.Status != BookingStatus.AcceptedByProvider)
            {
                return false;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledBy = cancelledBy;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkPaymentReceivedAsync(Guid bookingId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.Status = BookingStatus.PaymentReceived;
            booking.PaymentReceivedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkJobDoneAsync(Guid bookingId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.Status = BookingStatus.JobDone;
            booking.JobDoneAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkCompletedAsync(Guid bookingId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Validation
        public async Task<bool> IsBookingOwnerAsync(Guid bookingId, string userId)
        {
            return await _context.Bookings.AnyAsync(b => b.BookingId == bookingId && b.UserId == userId);
        }

        public async Task<bool> IsBookingProviderAsync(Guid bookingId, string providerId)
        {
            return await _context.Bookings.AnyAsync(b => b.BookingId == bookingId && b.ProviderId == providerId);
        }
    }
}
