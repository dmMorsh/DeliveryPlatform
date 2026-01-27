using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace CartService.Infrastructure.Outbox;

public class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(5000);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventProducer _producer;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, IEventProducer producer, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor fatal error");
            }

            await Task.Delay(PollDelay, stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();

        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT *
                    FROM "cart"."OutboxMessages" 
                    where "PublishedAt" IS NULL
                      AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= NOW())
                    ORDER BY "OccurredAt"
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
            """, BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var msg in messages)
        {
            try
            {
                await _producer.PublishAsync(topic: msg.Topic ?? "events", key: msg.AggregateId.ToString(), payload: msg.Payload, headers: new Dictionary<string,string>{{"event-type", msg.Type ?? ""}}, ct);
                msg.PublishedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.LastError = ex.Message;
                var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, msg.RetryCount), MaxRetryDelay.TotalSeconds));
                msg.NextRetryAt = DateTime.UtcNow.Add(delay);
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}, retry {RetryCount}", msg.Id, msg.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
