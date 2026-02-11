using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using OpenTelemetry;
using Serilog.Context;

namespace Shared.Services;

public static class CorrelationIdExtensions
{
    public const string HeaderName = "X-Correlation-Id";
    public const string PropertyName = "correlation.id";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
                ? headerValue.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = context.TraceIdentifier;
                context.Request.Headers[HeaderName] = correlationId;
            }

            if (Activity.Current != null)
            {
                Activity.Current.SetTag(PropertyName, correlationId);
            }

            Baggage.SetBaggage(PropertyName, correlationId);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty(PropertyName, correlationId))
            {
                await next();
            }
        });
    }
}
