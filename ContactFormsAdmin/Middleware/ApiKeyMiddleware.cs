using ContactFormsAdmin.Services;

namespace ContactFormsAdmin.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApiKeyService apiKeyService)
    {
        // Only check API key for /api/ routes (except webhook trigger - internal use)
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api/trigger-webhook"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key required. Include 'X-API-Key' header." });
            return;
        }

        var validKey = await apiKeyService.ValidateApiKeyAsync(apiKey!);
        if (validKey == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API key" });
            return;
        }

        await _next(context);
    }
}
