using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace CarRentalAPI.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(
            ApplicationDbContext context,
            ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to connect to database
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database");
                }

                // Try a simple query
                var userCount = await _context.Users.CountAsync(cancellationToken);
                var carCount = await _context.Cars.CountAsync(cancellationToken);
                var bookingCount = await _context.Bookings.CountAsync(cancellationToken);

                var data = new Dictionary<string, object>
                {
                    { "users", userCount },
                    { "cars", carCount },
                    { "bookings", bookingCount },
                    { "database", "connected" }
                };

                return HealthCheckResult.Healthy("Database is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                
                return HealthCheckResult.Unhealthy(
                    "Database health check failed",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "error", ex.Message }
                    });
            }
        }
    }
}