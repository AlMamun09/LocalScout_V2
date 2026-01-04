using LocalScout.Application.DTOs.AuditDTOs;
using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Repository interface for audit log operations
    /// </summary>
    public interface IAuditLogRepository
    {
        Task AddLogAsync(AuditLog log);
        Task<AuditLogPagedResultDto> GetLogsAsync(AuditLogFilterDto filter);
        Task<List<AuditLog>> GetLogsByUserAsync(string userId, int take = 50);
        Task<List<AuditLog>> GetLogsByEntityAsync(string entityType, string entityId, int take = 50);
        Task<List<string>> GetDistinctCategoriesAsync();
        Task<List<string>> GetDistinctActionsAsync();
    }
}
