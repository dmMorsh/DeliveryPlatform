using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

public sealed class ProcessedCommandCleanupHostedService<TDbContext, TEntity> : BackgroundService
    where TDbContext : DbContext
    where TEntity : class
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessedCommandCleanupHostedService<TDbContext, TEntity>> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public ProcessedCommandCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ProcessedCommandCleanupHostedService<TDbContext, TEntity>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = int.TryParse(config["Hangfire:ProcessedCommandRetentionDays"], out var days) ? days : 7;
        _interval = TimeSpan.FromHours(6);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Cleanup(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessedCommand cleanup failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task Cleanup(CancellationToken ct)
    {
        if (_retentionDays <= 0)
            return;

        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var deleted = await db.Set<TEntity>()
            .Where(e => EF.Property<DateTime>(e, "ProcessedAt") < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _logger.LogInformation("ProcessedCommand cleanup removed {Count} rows", deleted);
    }
}
