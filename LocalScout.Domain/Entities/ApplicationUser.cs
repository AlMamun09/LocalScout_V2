using Microsoft.AspNetCore.Identity;

namespace LocalScout.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime? DateOfBirth { get; set; } = DateTime.UtcNow;
        public string? WorkingDays { get; set; }
        public string? WorkingHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Gender { get; set; }
        public string? BusinessName { get; set; }
        public string? Description { get; set; }
        public bool IsVerified { get; set; } = false;
    }
}
