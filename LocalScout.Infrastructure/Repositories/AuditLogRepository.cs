using LocalScout.Application.DTOs.AuditDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogRepository> _logger;

        public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddLogAsync(AuditLog log)
        {
            log.AuditLogId = Guid.NewGuid();
            log.Timestamp = DateTime.UtcNow;
            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Audit log added: {log.Action} - {log.Category}");
        }

        public async Task<AuditLogPagedResultDto> GetLogsAsync(AuditLogFilterDto filter)
        {
            _logger.LogInformation($"GetLogsAsync called with filter: Search={filter.SearchQuery}, Category={filter.Category}, Action={filter.Action}, Page={filter.Page}");

            try
            {
                // First check if there are any audit logs at all
                var totalInDb = await _context.AuditLogs.CountAsync();
                _logger.LogInformation($"Total audit logs in database: {totalInDb}");

                var query = _context.AuditLogs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                {
                    var searchLower = filter.SearchQuery.ToLower();
                    query = query.Where(l => 
                        (l.UserName != null && l.UserName.ToLower().Contains(searchLower)) ||
                        (l.UserEmail != null && l.UserEmail.ToLower().Contains(searchLower)) ||
                        l.Action.ToLower().Contains(searchLower) ||
                        (l.Details != null && l.Details.ToLower().Contains(searchLower)));
                }

                if (!string.IsNullOrWhiteSpace(filter.Category))
                {
                    query = query.Where(l => l.Category == filter.Category);
                }

                if (!string.IsNullOrWhiteSpace(filter.Action))
                {
                    query = query.Where(l => l.Action == filter.Action);
                }

                if (!string.IsNullOrWhiteSpace(filter.UserId))
                {
                    query = query.Where(l => l.UserId == filter.UserId);
                }

                if (!string.IsNullOrWhiteSpace(filter.EntityType))
                {
                    query = query.Where(l => l.EntityType == filter.EntityType);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= filter.EndDate.Value);
                }

                if (filter.IsSuccess.HasValue)
                {
                    query = query.Where(l => l.IsSuccess == filter.IsSuccess.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();
                _logger.LogInformation($"Filtered count: {totalCount}");

                // Apply pagination
                var items = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(l => new AuditLogDto
                    {
                        AuditLogId = l.AuditLogId,
                        Timestamp = l.Timestamp,
                        UserId = l.UserId,
                        UserName = l.UserName,
                        UserEmail = l.UserEmail,
                        Action = l.Action,
                        Category = l.Category,
                        EntityType = l.EntityType,
                        EntityId = l.EntityId,
                        Details = l.Details,
                        IpAddress = l.IpAddress,
                        IsSuccess = l.IsSuccess
                    })
                    .ToListAsync();

                _logger.LogInformation($"Returning {items.Count} audit log items");

                return new AuditLogPagedResultDto
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    AppliedFilters = filter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLogsAsync");
                throw;
            }
        }

        public async Task<List<AuditLog>> GetLogsByUserAsync(string userId, int take = 50)
        {
            _logger.LogInformation($"GetLogsByUserAsync called for userId: {userId}, take: {take}");
            return await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, string entityId, int take = 50)
        {
            _logger.LogInformation($"GetLogsByEntityAsync called for entityType: {entityType}, entityId: {entityId}, take: {take}");
            return await _context.AuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctCategoriesAsync()
        {
            var categories = await _context.AuditLogs
                .Select(l => l.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            _logger.LogInformation($"Distinct categories retrieved: {string.Join(", ", categories)}");
            return categories;
        }

        public async Task<List<string>> GetDistinctActionsAsync()
        {
            var actions = await _context.AuditLogs
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            _logger.LogInformation($"Distinct actions retrieved: {string.Join(", ", actions)}");
            return actions;
        }
    }
}
