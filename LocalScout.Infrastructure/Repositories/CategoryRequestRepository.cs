using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    public class CategoryRequestRepository : ICategoryRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CategoryRequest> CreateRequestAsync(
            string providerId,
            string providerName,
            CategoryRequestDto dto)
        {
            var request = new CategoryRequest
            {
                CategoryRequestId = Guid.NewGuid(),
                ProviderId = providerId,
                ProviderName = providerName,
                RequestedCategoryName = dto.RequestedCategoryName,
                Description = dto.Description,
                Status = VerificationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.CategoryRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<CategoryRequest?> GetByIdAsync(Guid id)
        {
            return await _context.CategoryRequests.FindAsync(id);
        }

        public async Task<List<CategoryRequest>> GetPendingRequestsAsync()
        {
            return await _context.CategoryRequests
                .Where(r => r.Status == VerificationStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CategoryRequest>> GetRequestsByProviderIdAsync(string providerId)
        {
            return await _context.CategoryRequests
                .Where(r => r.ProviderId == providerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CategoryRequest>> GetRequestsByStatusAsync(VerificationStatus status)
        {
            return await _context.CategoryRequests
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(Guid requestId, VerificationStatus status, string? adminReason = null)
        {
            var request = await _context.CategoryRequests.FindAsync(requestId);
            if (request != null)
            {
                request.Status = status;
                request.ReviewedAt = DateTime.UtcNow;
                request.AdminReason = adminReason;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasPendingRequestAsync(string providerId, string categoryName)
        {
            return await _context.CategoryRequests.AnyAsync(r =>
                r.ProviderId == providerId &&
                r.RequestedCategoryName.ToLower() == categoryName.ToLower() &&
                r.Status == VerificationStatus.Pending);
        }
    }
}
