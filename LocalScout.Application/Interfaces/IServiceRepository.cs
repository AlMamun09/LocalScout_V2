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
        Task<IEnumerable<Service>> GetNearbyServicesAsync(double? userLatitude, double? userLongitude, int maxResults = 20);
        Task<IEnumerable<Service>> SearchServicesAsync(string? query, Guid? categoryId, int maxResults = 20);
        Task AddServiceAsync(Service service);
        Task UpdateServiceAsync(Service service);
        Task SoftDeleteServiceAsync(Guid id);
        Task<IEnumerable<Service>> GetOtherServicesByProviderAsync(string providerId, Guid excludeServiceId, int maxResults = 4);
        Task<IEnumerable<Service>> GetRelatedServicesAsync(Guid categoryId, string excludeProviderId, int maxResults = 6);
    }
}
