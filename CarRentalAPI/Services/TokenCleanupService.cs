using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarRentalAPI.Services
{
    /// <summary>
    /// Background service that periodically cleans up expired refresh tokens
    /// </summary>
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Run every 6 hours

        public TokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokens();
                    
                    // Wait for the next cleanup interval
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    _logger.LogInformation("Token Cleanup Service is stopping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during token cleanup");
                    
                    // Wait a bit before retrying
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Token Cleanup Service stopped");
        }

        private async Task CleanupExpiredTokens()
        {
            _logger.LogInformation("Starting token cleanup...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
                
                await tokenService.CleanupAllExpiredTokens();
                
                _logger.LogInformation("Token cleanup completed");
            }
        }
    }
}