using LocalScout.Application.DTOs;

namespace LocalScout.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(string userId);
        Task<bool> ToggleUserStatusAsync(string userId, string? blockReason = null);
        Task<IEnumerable<UserDto>> GetUsersByStatusAsync(bool isActive);
    }
}
