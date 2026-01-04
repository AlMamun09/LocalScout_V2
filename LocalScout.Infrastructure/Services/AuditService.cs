using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LocalScout.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditService(
            IAuditLogRepository auditLogRepository,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(
            string action,
            string category,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool isSuccess = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            string? userId = null;
            string? userName = null;
            string? userEmail = null;

            if (user?.Identity?.IsAuthenticated == true)
            {
                var appUser = await _userManager.GetUserAsync(user);
                if (appUser != null)
                {
                    userId = appUser.Id;
                    userName = appUser.FullName ?? appUser.UserName;
                    userEmail = appUser.Email;
                }
            }

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                Action = action,
                Category = category,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = GetIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext),
                IsSuccess = isSuccess
            };

            await _auditLogRepository.AddLogAsync(log);
        }

        public async Task LogAsync(
            string userId,
            string userName,
            string userEmail,
            string action,
            string category,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool isSuccess = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                UserEmail = userEmail,
                Action = action,
                Category = category,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = GetIpAddress(httpContext),
                UserAgent = GetUserAgent(httpContext),
                IsSuccess = isSuccess
            };

            await _auditLogRepository.AddLogAsync(log);
        }

        private static string? GetIpAddress(HttpContext? context)
        {
            if (context == null) return null;

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }

        private static string? GetUserAgent(HttpContext? context)
        {
            if (context == null) return null;
            
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            // Truncate if too long
            if (userAgent?.Length > 500)
            {
                userAgent = userAgent.Substring(0, 500);
            }
            return userAgent;
        }
    }
}
