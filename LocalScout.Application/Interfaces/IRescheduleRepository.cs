using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Repository for managing reschedule proposals
    /// </summary>
    public interface IRescheduleRepository
    {
        /// <summary>
        /// Get a proposal by its ID
        /// </summary>
        Task<RescheduleProposal?> GetByIdAsync(Guid proposalId);
        
        /// <summary>
        /// Get all proposals for a booking
        /// </summary>
        Task<List<RescheduleProposal>> GetByBookingIdAsync(Guid bookingId);
        
        /// <summary>
        /// Get pending proposals for a booking
        /// </summary>
        Task<List<RescheduleProposal>> GetPendingProposalsAsync(Guid bookingId);
        
        /// <summary>
        /// Get the latest proposal for a booking
        /// </summary>
        Task<RescheduleProposal?> GetLatestProposalAsync(Guid bookingId);
        
        /// <summary>
        /// Create a new proposal
        /// </summary>
        Task<RescheduleProposal> CreateAsync(RescheduleProposal proposal);
        
        /// <summary>
        /// Update proposal status
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid proposalId, RescheduleStatus status);
        
        /// <summary>
        /// Expire all pending proposals for a booking (when one is accepted)
        /// </summary>
        Task<int> ExpirePendingProposalsAsync(Guid bookingId, Guid? exceptProposalId = null);
    }
}
