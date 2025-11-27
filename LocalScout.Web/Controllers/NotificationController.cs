using LocalScout.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LocalScout.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationRepository notificationRepository, ILogger<NotificationController> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            _logger.LogInformation($"Fetching unread count for User ID: {userId}");
            var count = await _notificationRepository.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetNotifications([FromQuery] int take = 50)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            _logger.LogInformation($"Fetching notifications for User ID: {userId}");
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, take);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotification(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return NotFound();

            // Verify notification belongs to user
            if (notification.UserId != userId)
                return Forbid();

            return Ok(notification);
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Verify notification belongs to user
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return NotFound();

            if (notification.UserId != userId)
                return Forbid();

            var success = await _notificationRepository.MarkAsReadAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to mark notification as read" });

            // Get updated count
            var count = await _notificationRepository.GetUnreadCountAsync(userId);
            return Ok(new { success = true, unreadCount = count });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var success = await _notificationRepository.MarkAllAsReadAsync(userId);
            if (!success)
                return BadRequest(new { message = "Failed to mark all notifications as read" });

            return Ok(new { success = true, unreadCount = 0 });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Verify notification belongs to user
            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
                return NotFound();

            if (notification.UserId != userId)
                return Forbid();

            var success = await _notificationRepository.DeleteNotificationAsync(id);
            if (!success)
                return BadRequest(new { message = "Failed to delete notification" });

            // Get updated count
            var count = await _notificationRepository.GetUnreadCountAsync(userId);
            return Ok(new { success = true, unreadCount = count });
        }
    }
}
