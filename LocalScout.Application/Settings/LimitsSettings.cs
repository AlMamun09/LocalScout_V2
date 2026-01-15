namespace LocalScout.Application.Settings
{
    /// <summary>
    /// Configuration settings for platform limits and quotas
    /// </summary>
    public class LimitsSettings
    {
        public ProviderLimits Provider { get; set; } = new();
        public UserLimits User { get; set; } = new();
    }

    public class ProviderLimits
    {
        /// <summary>
        /// Maximum number of active services a provider can have
        /// </summary>
        public int MaxActiveServices { get; set; } = 5;

        /// <summary>
        /// Maximum number of concurrent accepted bookings (AcceptedByProvider, PaymentReceived, InProgress)
        /// </summary>
        public int MaxAcceptedBookings { get; set; } = 3;

        /// <summary>
        /// Maximum number of pending requests per service
        /// </summary>
        public int MaxPendingRequestsPerService { get; set; } = 5;
    }

    public class UserLimits
    {
        /// <summary>
        /// Maximum number of active bookings a user can have
        /// </summary>
        public int MaxActiveBookings { get; set; } = 5;

        /// <summary>
        /// Maximum number of total pending requests across all providers
        /// </summary>
        public int MaxPendingRequestsTotal { get; set; } = 10;

        /// <summary>
        /// Maximum number of cancellations allowed per month
        /// </summary>
        public int MaxCancellationsPerMonth { get; set; } = 5;

        /// <summary>
        /// Maximum number of active bookings with the same provider
        /// </summary>
        public int MaxActiveBookingsWithSameProvider { get; set; } = 3;
    }
}
