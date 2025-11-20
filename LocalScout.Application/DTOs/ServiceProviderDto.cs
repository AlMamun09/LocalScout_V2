namespace LocalScout.Application.DTOs
{
    public class ServiceProviderDto : UserDto
    {
        public string? BusinessName { get; set; }
        public string? Description { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerificationStatus { get; set; }
        public int TotalServices { get; set; }
        public int TotalBookings { get; set; }
        public double AverageRating { get; set; }
    }
}
