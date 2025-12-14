using LocalScout.Application.DTOs;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;

namespace LocalScout.Application.Interfaces
{
    public interface ICategoryRequestRepository
    {
        Task<CategoryRequest> CreateRequestAsync(string providerId, string providerName, CategoryRequestDto dto);
        Task<CategoryRequest?> GetByIdAsync(Guid id);
        Task<List<CategoryRequest>> GetPendingRequestsAsync();
        Task<List<CategoryRequest>> GetRequestsByProviderIdAsync(string providerId);
        Task<List<CategoryRequest>> GetRequestsByStatusAsync(VerificationStatus status);
        Task UpdateStatusAsync(Guid requestId, VerificationStatus status, string? adminReason = null);
        Task<bool> HasPendingRequestAsync(string providerId, string categoryName);
    }
}
