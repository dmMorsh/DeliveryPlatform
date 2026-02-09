using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Sharding;
using Shared.Services;

namespace PaymentService.Infrastructure.Outbox;

public class OutboxProcessor : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(5000);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    private readonly IPaymentDbContextFactory _dbFactory;
    private readonly IPaymentShardRouter _router;
    private readonly IEventProducer _producer;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IPaymentDbContextFactory dbFactory,
        IPaymentShardRouter router,
        IEventProducer producer,
        ILogger<OutboxProcessor> logger)
    {
        _dbFactory = dbFactory;
        _router = router;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllShards(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor fatal error");
            }

            await Task.Delay(PollDelay, stoppingToken);
        }
    }

    private async Task ProcessAllShards(CancellationToken ct)
    {
        foreach (var connectionString in _router.GetAllConnectionStrings())
        {
            await using var db = _dbFactory.Create(connectionString);
            await ProcessShard(db, ct);
        }
    }

    private async Task ProcessShard(PaymentDbContext db, CancellationToken ct)
    {
        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT *
                    FROM "payment"."OutboxMessages" 
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
                await _producer.PublishAsync(
                    topic: msg.Topic ?? "payment.events",
                    key: msg.AggregateId.ToString(),
                    payload: msg.Payload,
                    headers: new Dictionary<string, string>
                    {
                        { "event-id", msg.EventId },
                        { "event-type", msg.Type ?? "" }
                    },
                    ct: ct);
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
