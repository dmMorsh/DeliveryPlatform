using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Background service that monitors downstream health and adjusts rate limiter.
/// If outbox lag grows or dependencies degrade, it reduces the permit limit to throttle inbound traffic.
/// </summary>
public class AdaptiveThrottleService : BackgroundService
{
    private readonly IDistributedRateLimiter _limiter;
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<AdaptiveThrottleService> _logger;
    private readonly Func<Task<int>>? _getOutboxCountAsync;
    private int _maxPermits = 1000;
    private int _minPermits = 100;
    private const int CheckIntervalMs = 5000; // Every 5 seconds

    public AdaptiveThrottleService(
        IDistributedRateLimiter limiter,
        HealthCheckService healthCheckService,
        ILogger<AdaptiveThrottleService> logger,
        Func<Task<int>>? getOutboxCountAsync = null)
    {
        _limiter = limiter;
        _healthCheckService = healthCheckService;
        _logger = logger;
        _getOutboxCountAsync = getOutboxCountAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdaptiveThrottleService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AdjustThrottlingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in throttle adjustment");
            }

            await Task.Delay(CheckIntervalMs, stoppingToken);
        }
    }

    private async Task AdjustThrottlingAsync(CancellationToken ct)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            predicate: x => x.Tags.Contains("ready"),
            cancellationToken: ct);

        int newLimit = _limiter.GetCurrentPermitLimit();

        // Scale down if any health check is unhealthy
        if (report.Status == HealthStatus.Unhealthy)
        {
            newLimit = (int)(_limiter.GetCurrentPermitLimit() * 0.5); // 50% reduction
            _logger.LogWarning("Service unhealthy – reducing rate limit to {Limit}", newLimit);
        }
        // Scale down if degraded
        else if (report.Status == HealthStatus.Degraded)
        {
            newLimit = (int)(_limiter.GetCurrentPermitLimit() * 0.75); // 25% reduction
            _logger.LogWarning("Service degraded – reducing rate limit to {Limit}", newLimit);
        }
        // Scale up if recovering
        else if (report.Status == HealthStatus.Healthy && _limiter.GetCurrentPermitLimit() < _maxPermits)
        {
            newLimit = Math.Min((int)(_limiter.GetCurrentPermitLimit() * 1.1), _maxPermits); // 10% increase
            _logger.LogInformation("Service recovered – increasing rate limit to {Limit}", newLimit);
        }

        // Also check outbox lag manually if available
        if (_getOutboxCountAsync != null)
        {
            try
            {
                int outboxCount = await _getOutboxCountAsync();
                if (outboxCount > 500)
                {
                    newLimit = Math.Min(newLimit, 200);
                    _logger.LogWarning("High outbox lag ({Count}) – capping rate limit to {Limit}", outboxCount, newLimit);
                }
                else if (outboxCount > 100)
                {
                    newLimit = Math.Min(newLimit, 500);
                    _logger.LogWarning("Moderate outbox lag ({Count}) – reducing rate limit to {Limit}", outboxCount, newLimit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not fetch outbox count");
            }
        }

        newLimit = Math.Max(_minPermits, Math.Min(newLimit, _maxPermits));

        if (newLimit != _limiter.GetCurrentPermitLimit())
        {
            _limiter.SetPermitLimit(newLimit);
        }
    }
}
