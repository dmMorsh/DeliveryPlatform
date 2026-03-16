using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Shared.Services;

public sealed class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(5000);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventProducer _producer;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly string _schema;
    private readonly string _defaultTopic;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IEventProducer producer,
        ILogger<OutboxProcessor<TDbContext>> logger,
        string schema,
        string? defaultTopic = null)
    {
        if (string.IsNullOrWhiteSpace(schema))
            throw new ArgumentException("Schema name is required.", nameof(schema));

        _scopeFactory = scopeFactory;
        _producer = producer;
        _logger = logger;
        _schema = schema;
        _defaultTopic = string.IsNullOrWhiteSpace(defaultTopic) ? "events" : defaultTopic;
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
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var sql = $"""
            SELECT *
                FROM "{_schema}"."OutboxMessages" 
                where "PublishedAt" IS NULL
                  AND ("NextRetryAt" IS NULL OR "NextRetryAt" <= NOW())
                ORDER BY "OccurredAt"
            LIMIT {BatchSize}
            FOR UPDATE SKIP LOCKED
        """;

        var messages = await db.Set<OutboxMessage>()
            .FromSqlRaw(sql)
            .TagWith("INFRA_BACKGROUND_POLL")
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var msg in messages)
        {
            try
            {
                await _producer.PublishAsync(
                    topic: msg.Topic ?? _defaultTopic,
                    key: msg.AggregateId.ToString(),
                    payload: msg.Payload,
                    headers: new Dictionary<string, string>
                    {
                        ["event-id"] = msg.EventId,
                        ["event-type"] = msg.Type,
                        ["occurred-at"] = msg.OccurredAt.ToString("O")
                    },
                    ct);
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
