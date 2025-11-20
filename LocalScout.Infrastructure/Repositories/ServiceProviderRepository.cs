using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Identity;

namespace LocalScout.Infrastructure.Repositories
{
    public class ServiceProviderRepository : IServiceProviderRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceProviderRepository(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetAllProvidersAsync()
        {
            // Get all users in the ServiceProvider role
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);

            // Map to DTOs
            return providers.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetProvidersByStatusAsync(bool isActive)
        {
            // Get all ServiceProviders
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);

            // Filter by IsActive status only (regardless of verification)
            var filteredProviders = providers.Where(p => p.IsActive == isActive);

            // Map to DTOs
            return filteredProviders.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetVerificationRequestsAsync()
        {
            // Get all ServiceProviders
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);

            // Filter for providers that are NOT verified yet (pending verification)
            var pendingProviders = providers.Where(p => !p.IsVerified);

            return pendingProviders.Select(MapToDto).ToList();
        }

        public async Task<ServiceProviderDto?> GetProviderByIdAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return null;

            var roles = await _userManager.GetRolesAsync(provider);

            // Check if user is a service provider
            if (!roles.Contains(RoleNames.ServiceProvider))
                return null;

            return MapToDto(provider);
        }

        public async Task<bool> ToggleProviderStatusAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            // Toggle the IsActive status (Block/Unblock)
            provider.IsActive = !provider.IsActive;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        public async Task<bool> ApproveProviderAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            // Set verification status to approved
            provider.IsVerified = true;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        public async Task<bool> RejectProviderAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            // Keep IsVerified as false and block the provider
            provider.IsVerified = false;
            provider.IsActive = false;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        // Helper to map Entity to DTO
        private static ServiceProviderDto MapToDto(ApplicationUser provider)
        {
            // Determine verification status based on IsVerified flag
            string verificationStatus;
            if (provider.IsVerified)
            {
                verificationStatus = "Approved";
            }
            else
            {
                verificationStatus = "Pending";
            }

            return new ServiceProviderDto
            {
                Id = provider.Id,
                FullName = provider.FullName,
                Email = provider.Email,
                PhoneNumber = provider.PhoneNumber ?? "N/A",
                ProfilePictureUrl = provider.ProfilePictureUrl,
                IsActive = provider.IsActive,
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt,
                Address = provider.Address ?? "Not Provided",
                Gender = provider.Gender ?? "N/A",
                EmailConfirmed = provider.EmailConfirmed,
                Role = RoleNames.ServiceProvider,
                BusinessName = provider.BusinessName ?? "N/A",
                Description = provider.Description ?? "No description provided",
                IsVerified = provider.IsVerified,
                VerifiedAt = provider.IsVerified ? provider.UpdatedAt : null,
                VerificationStatus = verificationStatus,
                TotalServices = 0, // TODO: Implement when Services module is ready
                TotalBookings = 0, // TODO: Implement when Bookings module is ready
                AverageRating = 0.0 // TODO: Implement when Reviews module is ready
            };
        }
    }
}
