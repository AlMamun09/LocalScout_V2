using LocalScout.Application.DTOs;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LocalScout.Application.Interfaces
{
    public interface IVerificationRepository
    {
        Task<string?> ValidateSubmissionAsync(string providerId, IFormFile document);
        Task SubmitRequestAsync(string providerId, VerificationSubmissionDto dto, string webRootPath);
        Task<List<VerificationRequest>> GetPendingRequestsAsync();
        Task<VerificationRequest?> GetRequestByIdAsync(Guid requestId);
        Task<VerificationRequest?> GetLatestRequestByProviderIdAsync(string providerId);
        Task UpdateRequestStatusAsync(
            Guid requestId,
            VerificationStatus status,
            string? adminComments = null
        );
    }
}
