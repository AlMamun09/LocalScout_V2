using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for managing service blocks (temporary blocking after auto-cancellations)
    /// </summary>
    public class ServiceBlockRepository : IServiceBlockRepository
    {
        private readonly ApplicationDbContext _context;

        public ServiceBlockRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceBlock?> GetActiveBlockAsync(Guid serviceId)
        {
            return await _context.ServiceBlocks
                .FirstOrDefaultAsync(sb => sb.ServiceId == serviceId && sb.IsActive);
        }

        public async Task<bool> IsServiceBlockedAsync(Guid serviceId)
        {
            return await _context.ServiceBlocks
                .AnyAsync(sb => sb.ServiceId == serviceId && sb.IsActive && sb.UnblockAt > DateTime.Now);
        }

        public async Task<ServiceBlock> CreateAsync(ServiceBlock block)
        {
            block.ServiceBlockId = Guid.NewGuid();
            block.BlockedAt = DateTime.Now;
            block.IsActive = true;
            
            _context.ServiceBlocks.Add(block);
            await _context.SaveChangesAsync();
            
            return block;
        }

        public async Task<ServiceBlock> BlockServiceAsync(Guid serviceId, string reason, TimeSpan duration)
        {
            var block = new ServiceBlock
            {
                ServiceBlockId = Guid.NewGuid(),
                ServiceId = serviceId,
                Reason = reason,
                BlockedAt = DateTime.Now,
                UnblockAt = DateTime.Now.Add(duration),
                IsActive = true
            };

            _context.ServiceBlocks.Add(block);
            await _context.SaveChangesAsync();

            return block;
        }

        public async Task<bool> UnblockServiceAsync(Guid serviceId)
        {
            var block = await _context.ServiceBlocks
                .FirstOrDefaultAsync(sb => sb.ServiceId == serviceId && sb.IsActive);

            if (block == null) return false;

            block.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAsync(Guid serviceBlockId)
        {
            var block = await _context.ServiceBlocks
                .FirstOrDefaultAsync(sb => sb.ServiceBlockId == serviceBlockId);

            if (block == null) return false;

            block.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ServiceBlock>> GetExpiredBlocksAsync()
        {
            return await _context.ServiceBlocks
                .Where(sb => sb.IsActive && sb.UnblockAt <= DateTime.Now)
                .ToListAsync();
        }

        public async Task<int> DeactivateExpiredBlocksAsync()
        {
            var expiredBlocks = await _context.ServiceBlocks
                .Where(sb => sb.IsActive && sb.UnblockAt <= DateTime.Now)
                .ToListAsync();

            foreach (var block in expiredBlocks)
            {
                block.IsActive = false;
            }

            await _context.SaveChangesAsync();
            return expiredBlocks.Count;
        }
    }
}

