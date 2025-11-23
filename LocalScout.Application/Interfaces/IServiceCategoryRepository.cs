    using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    public interface IServiceCategoryRepository
    {
        Task<IEnumerable<ServiceCategory>> GetAllCategoryAsync();
        Task<IEnumerable<ServiceCategory>> GetActiveAndApprovedCategoryAsync();
        Task<IEnumerable<ServiceCategory>> GetCategoriesByStatusAsync(bool isActive, bool isApproved);
        Task<ServiceCategory?> GetCategoryByIdAsync(Guid id);
        Task AddCategoryAsync(ServiceCategory category);
        Task UpdateCategoryAsync(ServiceCategory category);
    }
}
