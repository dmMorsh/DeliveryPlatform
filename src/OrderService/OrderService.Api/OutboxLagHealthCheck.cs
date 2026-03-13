using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Api;

public class OutboxLagHealthCheck : IHealthCheck
{
    private readonly OrderDbContext _db;

    public OutboxLagHealthCheck(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var count = await _db.OutboxMessages.Where(x => x.PublishedAt == null).CountAsync(cancellationToken);

        if (count > 500)
            return HealthCheckResult.Unhealthy($"Outbox lag critical: {count}");

        if (count > 100)
            return HealthCheckResult.Degraded($"Outbox lag warning: {count}");

        return HealthCheckResult.Healthy($"Outbox OK: {count}");
    }
}