using LocalScout.Application.Extensions;

namespace LocalScout.Application.DTOs.ReviewDTOs
{
    /// <summary>
    /// DTO for creating a new review
    /// </summary>
    public class ReviewCreateDto
    {
        public Guid BookingId { get; set; }
        public int Rating { get; set; }  // 1-5
        public string? Comment { get; set; }
    }

    /// <summary>
    /// DTO for displaying a review
    /// </summary>
    public class ReviewDisplayDto
    {
        public Guid ReviewId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserProfilePicture { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted => GetTimeAgo();

        private string GetTimeAgo()
        {
            // Convert to BD time for calculation
            var createdAtBd = CreatedAt.ToBdTime();
            var nowBd = DateTime.UtcNow.ToBdTime();
            var timeSpan = nowBd - createdAtBd;
            
            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays >= 7)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
            return "Just now";
        }
    }

    /// <summary>
    /// DTO for service rating summary
    /// </summary>
    public class ServiceRatingDto
    {
        public Guid ServiceId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }

        // Percentage helpers for progress bars
        public int FiveStarPercent => TotalReviews > 0 ? (int)((FiveStarCount * 100.0) / TotalReviews) : 0;
        public int FourStarPercent => TotalReviews > 0 ? (int)((FourStarCount * 100.0) / TotalReviews) : 0;
        public int ThreeStarPercent => TotalReviews > 0 ? (int)((ThreeStarCount * 100.0) / TotalReviews) : 0;
        public int TwoStarPercent => TotalReviews > 0 ? (int)((TwoStarCount * 100.0) / TotalReviews) : 0;
        public int OneStarPercent => TotalReviews > 0 ? (int)((OneStarCount * 100.0) / TotalReviews) : 0;
    }
}
