using System.Net;
using System.Text.Json;
using System.Globalization;

namespace ITBS_Classroom.Middleware;

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
            _logger.LogError(ex, "Unhandled exception");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var payload = JsonSerializer.Serialize(new
            {
                message = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                    ? "Une erreur inattendue est survenue."
                    : "An unexpected error occurred."
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
