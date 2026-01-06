using LocalScout.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Services
{
    /// <summary>
    /// Background service that automatically unblocks services when their
    /// block period has expired.
    /// </summary>
    public class ServiceUnblockService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceUnblockService> _logger;
        
        // Check every 15 minutes for expired blocks
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

        public ServiceUnblockService(
            IServiceProvider serviceProvider,
            ILogger<ServiceUnblockService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ServiceUnblockService started. Checking every {Interval} minutes.", 
                CheckInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UnblockExpiredServicesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired service blocks");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("ServiceUnblockService stopped.");
        }

        private async Task UnblockExpiredServicesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var serviceBlockRepository = scope.ServiceProvider.GetRequiredService<IServiceBlockRepository>();

            var expiredBlocks = await serviceBlockRepository.GetExpiredBlocksAsync();

            if (!expiredBlocks.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} expired service blocks to remove.", expiredBlocks.Count);

            foreach (var block in expiredBlocks)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await serviceBlockRepository.UnblockServiceAsync(block.ServiceId);
                    _logger.LogInformation("Unblocked service {ServiceId}. Block reason was: {Reason}", 
                        block.ServiceId, block.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unblocking service {ServiceId}", block.ServiceId);
                }
            }
        }
    }
}
