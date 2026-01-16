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
            _logger.LogInformation($"GetLogsAsync called with filter: Page={filter.Page}");

            try
            {
                var page = filter.Page < 1 ? 1 : filter.Page;
                var pageSize = filter.PageSize <= 0 ? 25 : filter.PageSize;

                var query = _context.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.Category))
                {
                    query = query.Where(x => x.Category == filter.Category);
                }

                if (!string.IsNullOrWhiteSpace(filter.Action))
                {
                    query = query.Where(x => x.Action == filter.Action);
                }

                if (!string.IsNullOrWhiteSpace(filter.UserId))
                {
                    query = query.Where(x => x.UserId == filter.UserId);
                }

                if (!string.IsNullOrWhiteSpace(filter.EntityType))
                {
                    query = query.Where(x => x.EntityType == filter.EntityType);
                }

                if (filter.IsSuccess.HasValue)
                {
                    query = query.Where(x => x.IsSuccess == filter.IsSuccess.Value);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(x => x.Timestamp <= filter.EndDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                {
                    var search = filter.SearchQuery.Trim();
                    query = query.Where(x =>
                        (x.UserName != null && x.UserName.Contains(search)) ||
                        (x.UserEmail != null && x.UserEmail.Contains(search)) ||
                        x.Action.Contains(search) ||
                        (x.Details != null && x.Details.Contains(search)));
                }

                var totalCount = filter.SkipCount ? -1 : await query.CountAsync();

                var items = await query
                    .OrderByDescending(x => x.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                    Page = page,
                    PageSize = pageSize,
                    AppliedFilters = filter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetLogsAsync");
                throw;
            }
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.AuditLogs.AsNoTracking().CountAsync();
        }

        public async Task<List<AuditLog>> GetLogsByUserAsync(string userId, int take = 50)
        {
            _logger.LogInformation($"GetLogsByUserAsync called for userId: {userId}, take: {take}");
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, string entityId, int take = 50)
        {
            _logger.LogInformation($"GetLogsByEntityAsync called for entityType: {entityType}, entityId: {entityId}, take: {take}");
            return await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctCategoriesAsync()
        {
            var categories = await _context.AuditLogs
                .AsNoTracking()
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
                .AsNoTracking()
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            _logger.LogInformation($"Distinct actions retrieved: {string.Join(", ", actions)}");
            return actions;
        }
    }
}
