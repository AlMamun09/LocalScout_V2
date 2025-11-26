using System;

namespace LocalScout.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty; // No navigation property - just scalar value
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? MetaJson { get; set; } // Optional JSON metadata for future extensibility
    }
}
