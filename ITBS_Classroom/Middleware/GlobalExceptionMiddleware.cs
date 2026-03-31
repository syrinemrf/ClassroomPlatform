using System.Net;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Http;

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

            // If the response has already started, we cannot modify it. Re-throw so the server can handle it.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the global exception handler will not be able to write the error response.");
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500; // Internal Server Error

            var message = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                ? "Une erreur inattendue est survenue."
                : "An unexpected error occurred.";

            var payload = JsonSerializer.Serialize(new { message }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
