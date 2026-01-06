namespace LocalScout.Application.Services
{
    /// <summary>
    /// Service for handling timezone conversions across the application
    /// </summary>
    public interface ITimeZoneService
    {
        /// <summary>
        /// Converts UTC DateTime to Bangladesh time (UTC+6)
        /// </summary>
        DateTime ConvertUtcToBdTime(DateTime utcDateTime);
        
        /// <summary>
        /// Converts Bangladesh time to UTC
        /// </summary>
        DateTime ConvertBdTimeToUtc(DateTime bdDateTime);
        
        /// <summary>
        /// Gets current Bangladesh time
        /// </summary>
        DateTime GetBangladeshNow();
        
        /// <summary>
        /// Formats a UTC DateTime as Bangladesh time string
        /// </summary>
        string FormatBdDateTime(DateTime utcDateTime, string format = "MMM dd, yyyy h:mm tt");
        
        /// <summary>
        /// Formats a UTC DateTime as Bangladesh date string
        /// </summary>
        string FormatBdDate(DateTime utcDateTime, string format = "MMM dd, yyyy");
        
        /// <summary>
        /// Formats a UTC DateTime as Bangladesh time string
        /// </summary>
        string FormatBdTime(DateTime utcDateTime, string format = "h:mm tt");
    }
}
