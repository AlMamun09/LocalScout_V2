namespace LocalScout.Application.DTOs.AuditDTOs
{
    /// <summary>
    /// DTO for displaying audit log entries
    /// </summary>
    public class AuditLogDto
    {
        public Guid AuditLogId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public bool IsSuccess { get; set; }
    }
}
