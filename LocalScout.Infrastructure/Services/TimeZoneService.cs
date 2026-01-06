using LocalScout.Application.Services;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Implementation of timezone service for Bangladesh (UTC+6)
    /// </summary>
    public class TimeZoneService : ITimeZoneService
    {
        private static readonly TimeZoneInfo BangladeshTimeZone;
        
        static TimeZoneService()
        {
            // Create a custom timezone for Bangladesh (UTC+6)
            // Bangladesh doesn't observe DST
            try
            {
                // Try to get the timezone by ID (works on Windows and some Linux systems)
                BangladeshTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            }
            catch
            {
                // Fallback: Create a custom timezone for UTC+6
                BangladeshTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                    "Bangladesh Standard Time",
                    TimeSpan.FromHours(6),
                    "Bangladesh Standard Time",
                    "Bangladesh Standard Time"
                );
            }
        }
        
        public DateTime ConvertUtcToBdTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, BangladeshTimeZone);
        }
        
        public DateTime ConvertBdTimeToUtc(DateTime bdDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(bdDateTime, BangladeshTimeZone);
        }
        
        public DateTime GetBangladeshNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BangladeshTimeZone);
        }
        
        public string FormatBdDateTime(DateTime utcDateTime, string format = "MMM dd, yyyy h:mm tt")
        {
            var bdTime = ConvertUtcToBdTime(utcDateTime);
            return bdTime.ToString(format);
        }
        
        public string FormatBdDate(DateTime utcDateTime, string format = "MMM dd, yyyy")
        {
            var bdTime = ConvertUtcToBdTime(utcDateTime);
            return bdTime.ToString(format);
        }
        
        public string FormatBdTime(DateTime utcDateTime, string format = "h:mm tt")
        {
            var bdTime = ConvertUtcToBdTime(utcDateTime);
            return bdTime.ToString(format);
        }
    }
}
