namespace LocalScout.Application.DTOs.AuditDTOs
{
    /// <summary>
    /// Filter parameters for querying audit logs
    /// </summary>
    public class AuditLogFilterDto
    {
        public string? SearchQuery { get; set; }
        public string? Category { get; set; }
        public string? Action { get; set; }
        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsSuccess { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
