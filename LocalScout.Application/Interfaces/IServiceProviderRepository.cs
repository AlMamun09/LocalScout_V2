using LocalScout.Application.DTOs;

namespace LocalScout.Application.Interfaces
{
    public interface IServiceProviderRepository
    {
        Task<IEnumerable<ServiceProviderDto>> GetAllProvidersAsync();
        Task<ServiceProviderDto?> GetProviderByIdAsync(string providerId);
        Task<bool> ToggleProviderStatusAsync(string providerId);
        Task<IEnumerable<ServiceProviderDto>> GetProvidersByStatusAsync(bool isActive);
        Task<IEnumerable<ServiceProviderDto>> GetVerificationRequestsAsync();
        Task<bool> ApproveProviderAsync(string providerId);
        Task<bool> RejectProviderAsync(string providerId);
        Task<ProviderDashboardDto> GetProviderDashboardAsync(string providerId);
    }
}
