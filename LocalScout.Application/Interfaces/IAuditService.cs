namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Service interface for logging audit events
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an audit event
        /// </summary>
        Task LogAsync(
            string action,
            string category,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool isSuccess = true);

        /// <summary>
        /// Log an audit event with specific user info (for when user context is not available)
        /// </summary>
        Task LogAsync(
            string userId,
            string userName,
            string userEmail,
            string action,
            string category,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool isSuccess = true);
    }
}
