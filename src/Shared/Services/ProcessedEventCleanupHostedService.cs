using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace Shared.Services;

public sealed class ProcessedEventCleanupHostedService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessedEventCleanupHostedService<TDbContext>> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public ProcessedEventCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ProcessedEventCleanupHostedService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = int.TryParse(config["Inbox:RetentionDays"], out var days) ? days : 7;
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
                _logger.LogError(ex, "ProcessedEvent cleanup failed");
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

        var query = db.Set<ProcessedEvent>()
            .Where(e => e.Status != "processing" &&
                        ((e.ProcessedAt ?? e.ReceivedAt) < cutoff));

        var deleted = await query.ExecuteDeleteAsync(ct);
        if (deleted > 0)
            _logger.LogInformation("ProcessedEvent cleanup removed {Count} rows", deleted);
    }
}
