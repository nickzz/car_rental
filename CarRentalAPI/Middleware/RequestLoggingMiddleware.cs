using System.Diagnostics;

namespace CarRentalAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            // Add request ID to response headers
            context.Response.Headers.Add("X-Request-Id", requestId);

            // Log request
            _logger.LogInformation(
                "HTTP {Method} {Path} started. RequestId: {RequestId}, IP: {IP}",
                context.Request.Method,
                context.Request.Path,
                requestId,
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            );

            try
            {
                await _next(context);

                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms. RequestId: {RequestId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    requestId
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "HTTP {Method} {Path} failed with exception after {ElapsedMs}ms. RequestId: {RequestId}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    requestId
                );

                throw; // Re-throw to be handled by GlobalExceptionHandler
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}