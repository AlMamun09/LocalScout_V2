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
                // Build raw SQL for maximum performance
                var offset = (filter.Page - 1) * filter.PageSize;
                
                var sql = @"
                    SELECT TOP (@pageSize) 
                        AuditLogId, Timestamp, UserId, UserName, UserEmail, 
                        Action, Category, EntityType, EntityId, Details, IpAddress, IsSuccess
                    FROM AuditLogs
                    WHERE 1=1";
                
                var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                {
                    new("@pageSize", filter.PageSize),
                    new("@offset", offset)
                };

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.Category))
                {
                    sql += " AND Category = @category";
                    parameters.Add(new("@category", filter.Category));
                }
                
                if (!string.IsNullOrWhiteSpace(filter.Action))
                {
                    sql += " AND Action = @action";
                    parameters.Add(new("@action", filter.Action));
                }
                
                if (!string.IsNullOrWhiteSpace(filter.UserId))
                {
                    sql += " AND UserId = @userId";
                    parameters.Add(new("@userId", filter.UserId));
                }
                
                if (filter.StartDate.HasValue)
                {
                    sql += " AND Timestamp >= @startDate";
                    parameters.Add(new("@startDate", filter.StartDate.Value));
                }
                
                if (filter.EndDate.HasValue)
                {
                    sql += " AND Timestamp <= @endDate";
                    parameters.Add(new("@endDate", filter.EndDate.Value));
                }
                
                if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
                {
                    sql += " AND (UserName LIKE @search OR UserEmail LIKE @search OR Action LIKE @search)";
                    parameters.Add(new("@search", $"%{filter.SearchQuery}%"));
                }

                sql += " ORDER BY Timestamp DESC";
                
                // Add OFFSET only if not first page
                if (filter.Page > 1)
                {
                    sql = sql.Replace("TOP (@pageSize)", "");
                    sql += " OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                }

                var items = await _context.AuditLogs
                    .FromSqlRaw(sql, parameters.ToArray())
                    .AsNoTracking()
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
                    TotalCount = -1, // Skip count for speed
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
