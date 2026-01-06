using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing provider time slots (locked availability)
    /// </summary>
    public class ProviderTimeSlotRepository : IProviderTimeSlotRepository
    {
        private readonly ApplicationDbContext _context;

        public ProviderTimeSlotRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProviderTimeSlot?> GetByIdAsync(Guid timeSlotId)
        {
            return await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.TimeSlotId == timeSlotId);
        }

        public async Task<ProviderTimeSlot?> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.BookingId == bookingId && ts.IsActive);
        }

        public async Task<List<ProviderTimeSlot>> GetProviderTimeSlotsAsync(string providerId, DateTime? fromDate = null)
        {
            var query = _context.ProviderTimeSlots
                .Where(ts => ts.ProviderId == providerId && ts.IsActive);

            if (fromDate.HasValue)
            {
                query = query.Where(ts => ts.EndDateTime >= fromDate.Value);
            }

            return await query
                .OrderBy(ts => ts.StartDateTime)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingSlotAsync(string providerId, DateTime startDateTime, DateTime endDateTime, Guid? excludeBookingId = null)
        {
            // Join with Bookings to ensure we only consider time slots for active (non-terminal) bookings
            // This handles cases where time slots weren't properly released for completed/cancelled bookings
            var query = from ts in _context.ProviderTimeSlots
                        join b in _context.Bookings on ts.BookingId equals b.BookingId
                        where ts.ProviderId == providerId 
                              && ts.IsActive
                              // Only consider bookings that are still active (not completed, cancelled, or auto-cancelled)
                              && b.Status != BookingStatus.Completed
                              && b.Status != BookingStatus.Cancelled
                              && b.Status != BookingStatus.AutoCancelled
                        select new { ts, b };

            if (excludeBookingId.HasValue)
            {
                query = query.Where(x => x.ts.BookingId != excludeBookingId.Value);
            }

            // Check for any overlap:
            // Overlap exists if: existing.Start < new.End AND existing.End > new.Start
            return await query.AnyAsync(x =>
                x.ts.StartDateTime < endDateTime && x.ts.EndDateTime > startDateTime);
        }

        public async Task<List<ProviderTimeSlot>> GetOverlappingSlotsAsync(string providerId, DateTime startDateTime, DateTime endDateTime)
        {
            // Join with Bookings to ensure we only consider time slots for active (non-terminal) bookings
            var query = from ts in _context.ProviderTimeSlots
                        join b in _context.Bookings on ts.BookingId equals b.BookingId
                        where ts.ProviderId == providerId 
                              && ts.IsActive
                              && b.Status != BookingStatus.Completed
                              && b.Status != BookingStatus.Cancelled
                              && b.Status != BookingStatus.AutoCancelled
                              && ts.StartDateTime < endDateTime 
                              && ts.EndDateTime > startDateTime
                        orderby ts.StartDateTime
                        select ts;

            return await query.ToListAsync();
        }

        public async Task<ProviderTimeSlot> CreateAsync(ProviderTimeSlot timeSlot)
        {
            timeSlot.TimeSlotId = Guid.NewGuid();
            timeSlot.CreatedAt = DateTime.Now;
            timeSlot.UpdatedAt = DateTime.Now;
            
            _context.ProviderTimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();
            
            return timeSlot;
        }

        public async Task<bool> UpdateAsync(ProviderTimeSlot timeSlot)
        {
            var existing = await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.TimeSlotId == timeSlot.TimeSlotId);

            if (existing == null) return false;

            existing.StartDateTime = timeSlot.StartDateTime;
            existing.EndDateTime = timeSlot.EndDateTime;
            existing.IsActive = timeSlot.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(Guid timeSlotId)
        {
            var timeSlot = await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.TimeSlotId == timeSlotId);

            if (timeSlot == null) return false;

            timeSlot.IsActive = false;
            timeSlot.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateByBookingIdAsync(Guid bookingId)
        {
            var timeSlot = await _context.ProviderTimeSlots
                .FirstOrDefaultAsync(ts => ts.BookingId == bookingId && ts.IsActive);

            if (timeSlot == null) return false;

            timeSlot.IsActive = false;
            timeSlot.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
