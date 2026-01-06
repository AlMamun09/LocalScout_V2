using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing reschedule proposals
    /// </summary>
    public class RescheduleRepository : IRescheduleRepository
    {
        private readonly ApplicationDbContext _context;

        public RescheduleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RescheduleProposal?> GetByIdAsync(Guid proposalId)
        {
            return await _context.RescheduleProposals
                .FirstOrDefaultAsync(rp => rp.ProposalId == proposalId);
        }

        public async Task<List<RescheduleProposal>> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.RescheduleProposals
                .Where(rp => rp.BookingId == bookingId)
                .OrderByDescending(rp => rp.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RescheduleProposal>> GetPendingProposalsAsync(Guid bookingId)
        {
            return await _context.RescheduleProposals
                .Where(rp => rp.BookingId == bookingId && rp.Status == RescheduleStatus.Pending)
                .OrderByDescending(rp => rp.CreatedAt)
                .ToListAsync();
        }

        public async Task<RescheduleProposal?> GetLatestProposalAsync(Guid bookingId)
        {
            return await _context.RescheduleProposals
                .Where(rp => rp.BookingId == bookingId)
                .OrderByDescending(rp => rp.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<RescheduleProposal> CreateAsync(RescheduleProposal proposal)
        {
            proposal.ProposalId = Guid.NewGuid();
            proposal.CreatedAt = DateTime.Now;
            proposal.Status = RescheduleStatus.Pending;
            
            _context.RescheduleProposals.Add(proposal);
            await _context.SaveChangesAsync();
            
            return proposal;
        }

        public async Task<bool> UpdateStatusAsync(Guid proposalId, RescheduleStatus status)
        {
            var proposal = await _context.RescheduleProposals
                .FirstOrDefaultAsync(rp => rp.ProposalId == proposalId);

            if (proposal == null) return false;

            proposal.Status = status;
            proposal.RespondedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ExpirePendingProposalsAsync(Guid bookingId, Guid? exceptProposalId = null)
        {
            var proposals = await _context.RescheduleProposals
                .Where(rp => rp.BookingId == bookingId && rp.Status == RescheduleStatus.Pending)
                .ToListAsync();

            if (exceptProposalId.HasValue)
            {
                proposals = proposals.Where(rp => rp.ProposalId != exceptProposalId.Value).ToList();
            }

            foreach (var proposal in proposals)
            {
                proposal.Status = RescheduleStatus.Expired;
                proposal.RespondedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return proposals.Count;
        }
    }
}
