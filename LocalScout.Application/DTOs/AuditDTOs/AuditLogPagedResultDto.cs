namespace LocalScout.Application.DTOs.AuditDTOs
{
    /// <summary>
    /// Paginated result for audit logs
    /// </summary>
    public class AuditLogPagedResultDto
    {
        public List<AuditLogDto> Items { get; set; } = new List<AuditLogDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        
        // Filter summary for display
        public AuditLogFilterDto? AppliedFilters { get; set; }
    }
}
