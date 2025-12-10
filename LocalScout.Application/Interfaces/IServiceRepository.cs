using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    public interface IServiceRepository
    {
        Task<Service?> GetServiceByIdAsync(Guid id);
        Task<IEnumerable<Service>> GetServiceByProviderAsync(string providerId, bool includeDeleted = false);
        Task<IEnumerable<Service>> GetActiveServicesByProviderAsync(string providerId);
        Task<IEnumerable<Service>> GetInactiveServicesByProviderAsync(string providerId);
        Task<IEnumerable<Service>> GetPublicActiveByCategoryAsync(Guid categoryId);
        Task AddServiceAsync(Service service);
        Task UpdateServiceAsync(Service service);
        Task SoftDeleteServiceAsync(Guid id);
    }
}
