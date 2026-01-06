using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants; // Ensure you have this for RoleNames
using Microsoft.AspNetCore.Identity;

namespace LocalScout.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            // Optimization: Directly fetch users in the "User" role
            var users = await _userManager.GetUsersInRoleAsync(RoleNames.User);

            // Map to DTOs
            return users.Select(u => MapToDto(u, RoleNames.User)).ToList();
        }

        public async Task<IEnumerable<UserDto>> GetUsersByStatusAsync(bool isActive)
        {
            // 1. Get all Regular Users
            var users = await _userManager.GetUsersInRoleAsync(RoleNames.User);

            // 2. Filter by IsActive status
            var filteredUsers = users.Where(u => u.IsActive == isActive);

            // 3. Map to DTOs
            return filteredUsers.Select(u => MapToDto(u, RoleNames.User)).ToList();
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            // Pass the first role found, or "User" as default
            return MapToDto(user, roles.FirstOrDefault() ?? RoleNames.User);
        }

        public async Task<bool> ToggleUserStatusAsync(string userId, string? blockReason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Toggle the boolean
            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Set or clear BlockReason based on new status
            if (!user.IsActive)
            {
                user.BlockReason = blockReason ?? "No reason provided";
            }
            else
            {
                user.BlockReason = null; // Clear reason when unblocking
            }

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        // Helper to map Entity to DTO
        private static UserDto MapToDto(ApplicationUser user, string role)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "N/A",
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Address = user.Address ?? "Not Provided",
                Gender = user.Gender ?? "N/A",
                EmailConfirmed = user.EmailConfirmed,
                Role = role,
                BlockReason = user.BlockReason
            };
        }
    }
}
