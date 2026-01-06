using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Repository for managing service blocks (temporary blocking after auto-cancellations)
    /// </summary>
    public interface IServiceBlockRepository
    {
        /// <summary>
        /// Get active block for a service
        /// </summary>
        Task<ServiceBlock?> GetActiveBlockAsync(Guid serviceId);
        
        /// <summary>
        /// Check if a service is currently blocked
        /// </summary>
        Task<bool> IsServiceBlockedAsync(Guid serviceId);
        
        /// <summary>
        /// Create a new service block
        /// </summary>
        Task<ServiceBlock> CreateAsync(ServiceBlock block);
        
        /// <summary>
        /// Block a service with reason and duration (convenience method)
        /// </summary>
        Task<ServiceBlock> BlockServiceAsync(Guid serviceId, string reason, TimeSpan duration);
        
        /// <summary>
        /// Unblock a service by service ID
        /// </summary>
        Task<bool> UnblockServiceAsync(Guid serviceId);
        
        /// <summary>
        /// Deactivate a block by block ID
        /// </summary>
        Task<bool> DeactivateAsync(Guid serviceBlockId);
        
        /// <summary>
        /// Get all expired blocks that need to be deactivated
        /// </summary>
        Task<List<ServiceBlock>> GetExpiredBlocksAsync();
        
        /// <summary>
        /// Deactivate all expired blocks
        /// </summary>
        Task<int> DeactivateExpiredBlocksAsync();
    }
}

