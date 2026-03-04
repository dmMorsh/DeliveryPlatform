using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

/// <summary>
/// Middleware that applies distributed rate limiting to incoming requests.
/// Returns 429 Too Many Requests when limit is exceeded.
/// </summary>
public class DistributedRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DistributedRateLimitMiddleware> _logger;

    public DistributedRateLimitMiddleware(RequestDelegate next, ILogger<DistributedRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, Services.IDistributedRateLimiter rateLimiter)
    {
        // Skip health checks to avoid self-blocking
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        if (!await rateLimiter.TryAcquireAsync())
        {
            _logger.LogWarning("Rate limit exceeded for {RemoteIp} {Method} {Path}",
                context.Connection.RemoteIpAddress, context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = "1";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded. Please retry after a delay." });
            return;
        }

        await _next(context);
    }
}

public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseDistributedRateLimit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DistributedRateLimitMiddleware>();
    }
}
