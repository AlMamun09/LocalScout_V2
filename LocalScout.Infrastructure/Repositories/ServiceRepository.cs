using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly ApplicationDbContext _context;

        public ServiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Service?> GetServiceByIdAsync(Guid id)
        {
            return await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id && !s.IsDeleted);
        }

        public async Task<IEnumerable<Service>> GetServiceByProviderAsync(string providerId, bool includeDeleted = false)
        {
            var query = _context.Services
                .Where(s => s.Id == providerId);

            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Service>> GetPublicActiveByCategoryAsync(Guid categoryId)
        {
            return await _context.Services
                .Where(s => s.ServiceCategoryId == categoryId
                    && s.IsActive
                    && !s.IsDeleted)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
        }

        public async Task AddServiceAsync(Service service)
        {
            service.ServiceId = Guid.NewGuid();
            service.CreatedAt = DateTime.UtcNow;
            service.UpdatedAt = DateTime.UtcNow;
            service.IsActive = true;
            service.IsDeleted = false;

            await _context.Services.AddAsync(service);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateServiceAsync(Service service)
        {
            service.UpdatedAt = DateTime.UtcNow;

            _context.Services.Update(service);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteServiceAsync(Guid id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service != null)
            {
                service.IsDeleted = true;
                service.IsActive = false;
                service.UpdatedAt = DateTime.UtcNow;

                _context.Services.Update(service);
                await _context.SaveChangesAsync();
            }
        }
    }
}