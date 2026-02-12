using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Shared.Services;

public static class HttpResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> CreatePolicyWrap(ILogger? logger = null)
    {
        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));

        var retry = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    logger?.LogWarning("HTTP retry {Attempt} after {Delay}. Status: {StatusCode}",
                        attempt, timespan, outcome.Result?.StatusCode);
                });

        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    logger?.LogWarning("HTTP circuit opened for {Duration}. Status: {StatusCode}",
                        duration, outcome.Result?.StatusCode);
                },
                onReset: () => logger?.LogInformation("HTTP circuit closed."),
                onHalfOpen: () => logger?.LogInformation("HTTP circuit half-open."));

        return Policy.WrapAsync(circuitBreaker, retry, timeout);
    }

    public static IAsyncPolicy CreateGrpcPolicyWrap(ILogger? logger = null)
    {
        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(10));

        var retry = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
                onRetry: (exception, timespan, attempt, _) =>
                {
                    logger?.LogWarning(exception, "gRPC retry {Attempt} after {Delay}", attempt, timespan);
                });

        var circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                {
                    logger?.LogWarning("gRPC circuit opened for {TimeSpan}", duration);
                },
                onReset: () => logger?.LogInformation("gRPC circuit closed."),
                onHalfOpen: () => logger?.LogInformation("gRPC circuit half-open."));

        return Policy.WrapAsync(circuitBreaker, retry, timeout);
    }
}
