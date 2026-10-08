using System.Net;

namespace AcxiomCRM.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled technical exception occurred during request processing.");

                // If API request, return clean JSON
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Message = "An unexpected server error occurred. Please contact system administrator."
                    });
                }
                else
                {
                    // Redirect MVC requests to generic error page without technical stack trace
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }
}
