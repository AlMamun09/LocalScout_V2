using LocalScout.Application.Services;

namespace LocalScout.Web.Extensions
{
    /// <summary>
    /// Extension methods for DateTime timezone conversions
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly Lazy<ITimeZoneService> _timeZoneService = 
            new Lazy<ITimeZoneService>(() => new LocalScout.Infrastructure.Services.TimeZoneService());
        
        /// <summary>
        /// Converts UTC DateTime to Bangladesh time
        /// </summary>
        public static DateTime ToBdTime(this DateTime utcDateTime)
        {
            return _timeZoneService.Value.ConvertUtcToBdTime(utcDateTime);
        }
        
        /// <summary>
        /// Converts UTC DateTime to Bangladesh time (nullable)
        /// </summary>
        public static DateTime? ToBdTime(this DateTime? utcDateTime)
        {
            return utcDateTime.HasValue ? _timeZoneService.Value.ConvertUtcToBdTime(utcDateTime.Value) : null;
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh time string
        /// </summary>
        public static string ToBdTimeString(this DateTime utcDateTime, string format = "MMM dd, yyyy h:mm tt")
        {
            return _timeZoneService.Value.FormatBdDateTime(utcDateTime, format);
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh time string (nullable)
        /// </summary>
        public static string ToBdTimeString(this DateTime? utcDateTime, string format = "MMM dd, yyyy h:mm tt")
        {
            return utcDateTime.HasValue ? _timeZoneService.Value.FormatBdDateTime(utcDateTime.Value, format) : "";
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh date string
        /// </summary>
        public static string ToBdDateString(this DateTime utcDateTime, string format = "MMM dd, yyyy")
        {
            return _timeZoneService.Value.FormatBdDate(utcDateTime, format);
        }
        
        /// <summary>
        /// Formats UTC DateTime as Bangladesh date string (nullable)
        /// </summary>
        public static string ToBdDateString(this DateTime? utcDateTime, string format = "MMM dd, yyyy")
        {
            return utcDateTime.HasValue ? _timeZoneService.Value.FormatBdDate(utcDateTime.Value, format) : "";
        }
    }
}
