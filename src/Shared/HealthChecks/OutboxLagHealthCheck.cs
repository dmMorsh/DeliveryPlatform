using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.HealthChecks;

/// <summary>
/// Health check that monitors outbox queue backlog.
/// Reports degraded if queue grows beyond threshold (indicates Kafka/producer lag).
/// </summary>
public class OutboxLagHealthCheck : IHealthCheck
{
    private readonly Func<Task<int>> _getOutboxCountAsync;
    private readonly int _warningThreshold;
    private readonly int _criticalThreshold;
    private readonly ILogger<OutboxLagHealthCheck> _logger;

    public OutboxLagHealthCheck(
        Func<Task<int>> getOutboxCountAsync,
        int warningThreshold = 100,
        int criticalThreshold = 500,
        ILogger<OutboxLagHealthCheck>? logger = null)
    {
        _getOutboxCountAsync = getOutboxCountAsync;
        _warningThreshold = warningThreshold;
        _criticalThreshold = criticalThreshold;
        _logger = logger ?? LoggerFactory.Create(b => { }).CreateLogger<OutboxLagHealthCheck>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _getOutboxCountAsync();
            
            if (count > _criticalThreshold)
            {
                _logger.LogWarning("Outbox critical: {Count} pending events (threshold: {Threshold})", count, _criticalThreshold);
                return HealthCheckResult.Unhealthy($"Outbox has {count} pending events (critical: {_criticalThreshold})");
            }

            if (count > _warningThreshold)
            {
                _logger.LogWarning("Outbox warning: {Count} pending events (threshold: {Threshold})", count, _warningThreshold);
                return HealthCheckResult.Degraded($"Outbox has {count} pending events (warning: {_warningThreshold})");
            }

            return HealthCheckResult.Healthy($"Outbox healthy: {count} events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking outbox health");
            return HealthCheckResult.Unhealthy("Failed to check outbox", ex);
        }
    }
}

public static class OutboxHealthCheckExtensions
{
    public static IHealthChecksBuilder AddOutboxLagCheck(
        this IHealthChecksBuilder builder,
        Func<Task<int>> getOutboxCountAsync,
        int? warningThreshold = null,
        int? criticalThreshold = null,
        string name = "outbox")
    {
        return builder.AddCheck(
            name,
            new OutboxLagHealthCheck(
                getOutboxCountAsync,
                warningThreshold ?? 100,
                criticalThreshold ?? 500),
            tags: new[] { "ready" });
    }
}
