using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    public class ServiceCategoryRepository : IServiceCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public ServiceCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ServiceCategory>> GetAllCategoryAsync()
        {
            // Returns all categories for the Admin Dashboard (Active & Inactive)
            return await _context
                .ServiceCategories.OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServiceCategory>> GetActiveAndApprovedCategoryAsync()
        {
            // Returns only valid categories for Users (Search) and Providers (Dropdowns)
            return await _context
                .ServiceCategories.Where(s => s.IsActive && s.IsApproved)
                .OrderBy(s => s.CategoryName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ServiceCategory>> GetCategoriesByStatusAsync(bool isActive, bool isApproved)
        {
            return await _context
                .ServiceCategories.Where(s => s.IsActive == isActive && s.IsApproved == isApproved)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<ServiceCategory?> GetCategoryByIdAsync(Guid id)
        {
            return await _context.ServiceCategories.FindAsync(id);
        }

        public async Task AddCategoryAsync(ServiceCategory category)
        {
            await _context.ServiceCategories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(ServiceCategory category)
        {
            _context.ServiceCategories.Update(category);
            await _context.SaveChangesAsync();
        }


    }
}
