using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LocalScout.Application.Services;

namespace LocalScout.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeZoneService _timeZoneService;

        public BookingRepository(ApplicationDbContext context, ITimeZoneService timeZoneService)
        {
            _context = context;
            _timeZoneService = timeZoneService;
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

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings.ToListAsync();
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
                            (b.Status == BookingStatus.PaymentReceived ||
                             b.Status == BookingStatus.JobDone ||
                             b.Status == BookingStatus.Completed) && 
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
                            (b.Status == BookingStatus.PaymentReceived ||
                             b.Status == BookingStatus.JobDone ||
                             b.Status == BookingStatus.Completed) && 
                            b.NegotiatedPrice.HasValue)
                .SumAsync(b => b.NegotiatedPrice!.Value);
        }

        // Status updates
        public async Task<bool> AcceptBookingAsync(Guid bookingId, decimal negotiatedPrice, string? providerNotes)
        {
            var booking = await GetByIdAsync(bookingId);
            // Allow accept for PendingProviderReview, NeedRescheduling, or user proposal (PendingProviderApproval)
            if (booking == null || 
                (booking.Status != BookingStatus.PendingProviderReview && 
                 booking.Status != BookingStatus.NeedRescheduling &&
                 booking.Status != BookingStatus.PendingProviderApproval))
            {
                return false;
            }

            booking.NegotiatedPrice = negotiatedPrice;
            booking.ProviderNotes = providerNotes;
            booking.Status = BookingStatus.AcceptedByProvider;
            booking.AcceptedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            
            // Clear proposal fields
            booking.ProposedStartDateTime = null;
            booking.ProposedEndDateTime = null;
            booking.ProposedPrice = null;
            booking.ProposedNotes = null;
            booking.ProposedBy = null;

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

            // Release time slot for terminal statuses
            if (newStatus == BookingStatus.Completed || 
                newStatus == BookingStatus.Cancelled || 
                newStatus == BookingStatus.AutoCancelled)
            {
                await ReleaseTimeSlotAsync(bookingId);
            }

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
                booking.Status != BookingStatus.AcceptedByProvider &&
                booking.Status != BookingStatus.NeedRescheduling)
            {
                return false;
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledBy = cancelledBy;
            booking.CancellationReason = reason;
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Release the time slot so it becomes available for new bookings
            await ReleaseTimeSlotAsync(bookingId);

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

        public async Task<bool> MarkPaymentReceivedAsync(Guid bookingId, string transactionId, string validationId, string paymentMethod, string? bankTxnId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.Status = BookingStatus.PaymentReceived;
            booking.PaymentReceivedAt = DateTime.UtcNow;
            booking.TransactionId = transactionId;
            booking.ValidationId = validationId;
            booking.PaymentMethod = paymentMethod;
            booking.BankTransactionId = bankTxnId;
            booking.PaymentStatus = "VALID";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Booking?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.TransactionId == transactionId);
        }

        public async Task<bool> SetTransactionIdAsync(Guid bookingId, string transactionId)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.TransactionId = transactionId;
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

            // Release the time slot so it becomes available for new bookings
            await ReleaseTimeSlotAsync(bookingId);

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

        public async Task<bool> HasPendingCompletionAsync(string userId)
        {
            return await _context.Bookings.AnyAsync(b => 
                b.UserId == userId && 
                b.Status == BookingStatus.JobDone);
        }

        public async Task<List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>> GetPaymentHistoryForUserAsync(string userId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId && 
                       (b.Status == BookingStatus.PaymentReceived || 
                        b.Status == BookingStatus.JobDone || 
                        b.Status == BookingStatus.Completed) &&
                       !string.IsNullOrEmpty(b.TransactionId))
                .OrderByDescending(b => b.PaymentReceivedAt)
                .ToListAsync();

            if (!bookings.Any())
                return new List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>();

            var serviceIds = bookings.Select(b => b.ServiceId).Distinct().ToList();
            var providerIds = bookings.Select(b => b.ProviderId).Distinct().ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceName);

            var providers = await _context.Users
                .Where(u => providerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.ProfilePictureUrl });

            return bookings.Select(b => new LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto
            {
                BookingId = b.BookingId,
                ServiceId = b.ServiceId,
                TransactionId = b.TransactionId,
                ValidationId = b.ValidationId,
                Amount = b.NegotiatedPrice ?? 0,
                PaymentMethod = b.PaymentMethod,
                PaymentDate = _timeZoneService.ConvertUtcToBdTime(b.PaymentReceivedAt ?? b.UpdatedAt),
                Status = b.PaymentStatus ?? "Success",
                ServiceName = services.ContainsKey(b.ServiceId) ? services[b.ServiceId] ?? "Unknown Service" : "Unknown Service",
                OtherPartyName = providers.ContainsKey(b.ProviderId) ? providers[b.ProviderId].FullName ?? "Unknown Provider" : "Unknown Provider",
                OtherPartyImage = providers.ContainsKey(b.ProviderId) ? providers[b.ProviderId].ProfilePictureUrl : null
            }).ToList();
        }

        public async Task<List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>> GetPaymentHistoryForProviderAsync(string providerId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.ProviderId == providerId && 
                       (b.Status == BookingStatus.PaymentReceived || 
                        b.Status == BookingStatus.JobDone || 
                        b.Status == BookingStatus.Completed) &&
                       !string.IsNullOrEmpty(b.TransactionId))
                .OrderByDescending(b => b.PaymentReceivedAt)
                .ToListAsync();

            if (!bookings.Any())
                return new List<LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto>();

            var serviceIds = bookings.Select(b => b.ServiceId).Distinct().ToList();
            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceName);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.ProfilePictureUrl });

            return bookings.Select(b => new LocalScout.Application.DTOs.PaymentDTOs.PaymentHistoryDto
            {
                BookingId = b.BookingId,
                ServiceId = b.ServiceId,
                TransactionId = b.TransactionId,
                ValidationId = b.ValidationId,
                Amount = b.NegotiatedPrice ?? 0,
                PaymentMethod = b.PaymentMethod,
                PaymentDate = _timeZoneService.ConvertUtcToBdTime(b.PaymentReceivedAt ?? b.UpdatedAt),
                Status = b.PaymentStatus ?? "Success",
                ServiceName = services.ContainsKey(b.ServiceId) ? services[b.ServiceId] ?? "Unknown Service" : "Unknown Service",
                OtherPartyName = users.ContainsKey(b.UserId) ? users[b.UserId].FullName ?? "Unknown User" : "Unknown User",
                OtherPartyImage = users.ContainsKey(b.UserId) ? users[b.UserId].ProfilePictureUrl : null
            }).ToList();
        }

        // Scheduling-related methods
        
        public async Task<bool> AcceptBookingAsync(Guid bookingId, decimal negotiatedPrice, string? providerNotes, 
            DateTime confirmedStartDateTime, DateTime confirmedEndDateTime)
        {
            var booking = await GetByIdAsync(bookingId);
            // Allow accept for PendingProviderReview, NeedRescheduling, or user proposal (PendingProviderApproval)
            if (booking == null || 
                (booking.Status != BookingStatus.PendingProviderReview && 
                 booking.Status != BookingStatus.NeedRescheduling &&
                 booking.Status != BookingStatus.PendingProviderApproval))
            {
                return false;
            }

            booking.NegotiatedPrice = negotiatedPrice;
            booking.ProviderNotes = providerNotes;
            booking.ConfirmedStartDateTime = confirmedStartDateTime;
            booking.ConfirmedEndDateTime = confirmedEndDateTime;
            booking.Status = BookingStatus.AcceptedByProvider;
            booking.AcceptedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;
            
            // Clear proposal fields
            booking.ProposedStartDateTime = null;
            booking.ProposedEndDateTime = null;
            booking.ProposedPrice = null;
            booking.ProposedNotes = null;
            booking.ProposedBy = null;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasActiveBookingForServiceAsync(string userId, Guid serviceId)
        {
            return await _context.Bookings.AnyAsync(b => 
                b.UserId == userId && 
                b.ServiceId == serviceId &&
                (b.Status == BookingStatus.PendingProviderReview ||
                 b.Status == BookingStatus.AcceptedByProvider ||
                 b.Status == BookingStatus.AwaitingPayment ||
                 b.Status == BookingStatus.PaymentReceived ||
                 b.Status == BookingStatus.InProgress ||
                 b.Status == BookingStatus.NeedRescheduling));
        }

        public async Task<List<Booking>> GetExpiredPendingBookingsAsync(TimeSpan timeout)
        {
            // Use UtcNow since CreatedAt is stored in UTC
            var cutoffTime = DateTime.UtcNow.Subtract(timeout);
            
            return await _context.Bookings
                .Where(b => b.Status == BookingStatus.PendingProviderReview &&
                           b.CreatedAt <= cutoffTime)
                .ToListAsync();
        }

        public async Task<int> GetAutoCancelCountForServiceAsync(Guid serviceId, TimeSpan withinPeriod)
        {
            // Use UtcNow since CancelledAt is stored in UTC
            var cutoffTime = DateTime.UtcNow.Subtract(withinPeriod);
            
            return await _context.Bookings.CountAsync(b => 
                b.ServiceId == serviceId &&
                b.Status == BookingStatus.AutoCancelled &&
                b.CancelledAt >= cutoffTime);
        }

        public async Task<bool> UpdateBookingTimeAsync(Guid bookingId, DateTime newStartDateTime, DateTime newEndDateTime)
        {
            var booking = await GetByIdAsync(bookingId);
            if (booking == null)
            {
                return false;
            }

            booking.ConfirmedStartDateTime = newStartDateTime;
            booking.ConfirmedEndDateTime = newEndDateTime;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Releases the time slot associated with a booking, making the time available for new bookings.
        /// Called when a booking reaches a terminal status (Completed, Cancelled, AutoCancelled).
        /// </summary>
        private async Task ReleaseTimeSlotAsync(Guid bookingId)
        {
            var timeSlot = await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.BookingId == bookingId && ts.IsActive);

            if (timeSlot != null)
            {
                timeSlot.IsActive = false;
                timeSlot.UpdatedAt = DateTime.UtcNow;
            }
        }

        #region Limit Checking Methods

        /// <summary>
        /// Get count of pending requests for a specific service
        /// </summary>
        public async Task<int> GetPendingRequestCountForServiceAsync(Guid serviceId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.ServiceId == serviceId && 
                b.Status == BookingStatus.PendingProviderReview);
        }

        /// <summary>
        /// Get count of provider's currently accepted bookings (AcceptedByProvider, PaymentReceived, InProgress)
        /// </summary>
        public async Task<int> GetProviderAcceptedBookingCountAsync(string providerId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.ProviderId == providerId && 
                (b.Status == BookingStatus.AcceptedByProvider ||
                 b.Status == BookingStatus.PaymentReceived ||
                 b.Status == BookingStatus.InProgress));
        }

        /// <summary>
        /// Get count of user's cancellations in the current month
        /// </summary>
        public async Task<int> GetUserMonthlyCancellationCountAsync(string userId)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return await _context.Bookings.CountAsync(b => 
                b.UserId == userId && 
                b.Status == BookingStatus.Cancelled &&
                b.CancelledBy == "User" &&
                b.CancelledAt >= startOfMonth);
        }

        /// <summary>
        /// Get count of user's active bookings with a specific provider
        /// </summary>
        public async Task<int> GetUserActiveBookingsWithProviderAsync(string userId, string providerId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.UserId == userId && 
                b.ProviderId == providerId &&
                b.Status != BookingStatus.Completed &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.AutoCancelled);
        }

        /// <summary>
        /// Get count of user's total pending requests (awaiting provider response)
        /// </summary>
        public async Task<int> GetUserPendingRequestCountAsync(string userId)
        {
            return await _context.Bookings.CountAsync(b => 
                b.UserId == userId && 
                (b.Status == BookingStatus.PendingProviderReview ||
                 b.Status == BookingStatus.PendingProviderApproval));
        }

        #endregion
    }
}

