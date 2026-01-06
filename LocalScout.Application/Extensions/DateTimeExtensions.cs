namespace LocalScout.Application.Extensions
{
    /// <summary>
    /// Extension methods for DateTime timezone conversions to Bangladesh time (UTC+6)
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo BangladeshTimeZone;
        
        static DateTimeExtensions()
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
        
        /// <summary>
        /// Converts UTC DateTime to Bangladesh time
        /// </summary>
        public static DateTime ToBdTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, BangladeshTimeZone);
        }
        
        /// <summary>
        /// Converts UTC DateTime to Bangladesh time (nullable)
        /// </summary>
        public static DateTime? ToBdTime(this DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
                return null;
                
            return utcDateTime.Value.ToBdTime();
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh time string
        /// </summary>
        public static string ToBdTimeString(this DateTime utcDateTime, string format = "MMM dd, yyyy h:mm tt")
        {
            var bdTime = utcDateTime.ToBdTime();
            return bdTime.ToString(format);
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh time string (nullable)
        /// </summary>
        public static string ToBdTimeString(this DateTime? utcDateTime, string format = "MMM dd, yyyy h:mm tt")
        {
            if (!utcDateTime.HasValue)
                return "";
                
            return utcDateTime.Value.ToBdTimeString(format);
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh date string
        /// </summary>
        public static string ToBdDateString(this DateTime utcDateTime, string format = "MMM dd, yyyy")
        {
            var bdTime = utcDateTime.ToBdTime();
            return bdTime.ToString(format);
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh date string (nullable)
        /// </summary>
        public static string ToBdDateString(this DateTime? utcDateTime, string format = "MMM dd, yyyy")
        {
            if (!utcDateTime.HasValue)
                return "";
                
            return utcDateTime.Value.ToBdDateString(format);
        }
        
        /// <summary>
        /// Gets current Bangladesh time
        /// </summary>
        public static DateTime GetBangladeshNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BangladeshTimeZone);
        }
    }
}
