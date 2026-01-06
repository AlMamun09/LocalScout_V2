using LocalScout.Application.Interfaces;
using LocalScout.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Background service that automatically cancels pending bookings after 12 hours 
    /// if the provider hasn't responded. Also handles service blocking after 3 consecutive
    /// auto-cancellations.
    /// </summary>
    public class BookingAutoCancelService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingAutoCancelService> _logger;
        
        // Configuration
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
        private static readonly TimeSpan AutoCancelTimeout = TimeSpan.FromHours(12); // 12 hours to respond
        private const int MaxAutoCancelWarnings = 3; // Block service after 3 auto-cancellations
        private static readonly TimeSpan ServiceBlockDuration = TimeSpan.FromDays(2); // Block for 2 days

        public BookingAutoCancelService(
            IServiceProvider serviceProvider,
            ILogger<BookingAutoCancelService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingAutoCancelService started. Checking every {Interval} minutes. Auto-cancel timeout: {Timeout} hours.", 
                CheckInterval.TotalMinutes, AutoCancelTimeout.TotalHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredBookingsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired bookings");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("BookingAutoCancelService stopped.");
        }

        private async Task ProcessExpiredBookingsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var serviceBlockRepository = scope.ServiceProvider.GetRequiredService<IServiceBlockRepository>();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Get all pending bookings that have exceeded the timeout
            var expiredBookings = await bookingRepository.GetExpiredPendingBookingsAsync(AutoCancelTimeout);

            if (!expiredBookings.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} expired pending bookings to process.", expiredBookings.Count);

            foreach (var booking in expiredBookings)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    // Update booking status to AutoCancelled
                    await bookingRepository.UpdateStatusAsync(booking.BookingId, BookingStatus.AutoCancelled);
                    
                    // Get the count of recent auto-cancellations for this service (within 7 days)
                    var autoCancelCount = await bookingRepository.GetAutoCancelCountForServiceAsync(booking.ServiceId, TimeSpan.FromDays(7)) + 1;
                    
                    _logger.LogWarning(
                        "Auto-cancelled booking {BookingId} for service {ServiceId}. Provider {ProviderId} did not respond within {Hours} hours. Count: {Count}/{Max}",
                        booking.BookingId, booking.ServiceId, booking.ProviderId, AutoCancelTimeout.TotalHours, autoCancelCount, MaxAutoCancelWarnings);

                    // Notify the user that their booking was auto-cancelled
                    await notificationRepository.CreateNotificationAsync(
                        booking.UserId,
                        "Booking Auto-Cancelled",
                        $"Your booking request was automatically cancelled because the provider did not respond within 12 hours. Please try booking another provider.",
                        null
                    );

                    // Notify the provider about the auto-cancellation
                    await notificationRepository.CreateNotificationAsync(
                        booking.ProviderId,
                        "Booking Auto-Cancelled - Action Required",
                        $"A booking request was automatically cancelled because you did not respond within 12 hours. Warning {autoCancelCount}/{MaxAutoCancelWarnings}. " +
                        (autoCancelCount >= MaxAutoCancelWarnings 
                            ? "Your service has been temporarily blocked for 2 days." 
                            : "Please respond to booking requests promptly to avoid service blocking."),
                        null
                    );

                    // Check if we need to block the service
                    if (autoCancelCount >= MaxAutoCancelWarnings)
                    {
                        var isBlocked = await serviceBlockRepository.IsServiceBlockedAsync(booking.ServiceId);
                        if (!isBlocked)
                        {
                            await serviceBlockRepository.BlockServiceAsync(
                                booking.ServiceId,
                                $"Auto-blocked: {MaxAutoCancelWarnings} consecutive bookings auto-cancelled due to provider non-response.",
                                ServiceBlockDuration
                            );

                            _logger.LogWarning(
                                "Service {ServiceId} has been blocked for {Days} days due to {Count} auto-cancellations.",
                                booking.ServiceId, ServiceBlockDuration.TotalDays, MaxAutoCancelWarnings);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-cancelling booking {BookingId}", booking.BookingId);
                }
            }
        }
    }
}
