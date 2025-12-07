namespace LocalScout.Domain.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? MetaJson { get; set; }
    }
}
