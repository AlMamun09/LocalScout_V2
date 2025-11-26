using LocalScout.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LocalScout.Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<NotificationDto?> GetByIdAsync(Guid id);
        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int take = 50);
        Task<int> GetUnreadCountAsync(string userId);
        Task<NotificationDto> CreateNotificationAsync(string userId, string title, string message, string? metaJson = null);
        Task<bool> MarkAsReadAsync(Guid notificationId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<bool> DeleteNotificationAsync(Guid notificationId);
    }
}
