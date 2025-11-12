using System.Net;
using System.Text.Json;
using CarRentalAPI.Exceptions;

namespace CarRentalAPI.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new ApiResponse();

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Success = false;
                    response.Message = notFoundEx.Message;
                    break;

                case BadRequestException badRequestEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Success = false;
                    response.Message = badRequestEx.Message;
                    break;

                case UnauthorizedException unauthorizedEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Success = false;
                    response.Message = unauthorizedEx.Message;
                    break;

                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Success = false;
                    response.Message = validationEx.Message;
                    response.Errors = validationEx.Errors;
                    break;

                case ConflictException conflictEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Success = false;
                    response.Message = conflictEx.Message;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Success = false;
                    response.Message = "An internal server error occurred";
                    
                    // Only show detailed error in development
                    if (_env.IsDevelopment())
                    {
                        response.Errors.Add(exception.Message);
                        response.Errors.Add(exception.StackTrace ?? "No stack trace available");
                    }
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var result = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(result);
        }
    }

    // Extension method to register middleware
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}