using System.Text;
using Confluent.Kafka;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("../../logs/DlqService-.log",
           rollingInterval: RollingInterval.Day,
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" });

builder.AddServiceTelemetry("dlq-service");

builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddSingleton<IDlqReader, DlqReader>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/live", () => Results.Ok())
   .WithTags("health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

app.MapGet("/dlq", async (IDlqReader reader, int? limit, CancellationToken ct) =>
{
    var items = await reader.ReadLatestAsync(limit ?? 50, ct);
    return Results.Ok(items);
});

app.MapPost("/dlq/requeue", async (DlqRequeueRequest request, IDlqReader reader, IEventProducer producer, CancellationToken ct) =>
{
    if (request.Partition < 0 || request.Offset < 0)
        return Results.BadRequest("partition and offset are required.");

    var item = await reader.ReadByOffsetAsync(request.Partition, request.Offset, ct);
    if (item is null)
        return Results.NotFound("message not found");

    var targetTopic = request.TargetTopic ?? item.OriginalTopic;
    if (string.IsNullOrWhiteSpace(targetTopic))
        return Results.BadRequest("original-topic header is missing and targetTopic not provided.");

    var headers = new Dictionary<string, string>(item.Headers)
    {
        ["requeued"] = "true"
    };

    await producer.PublishAsync(targetTopic, item.Key ?? string.Empty, item.Payload, headers, ct);
    return Results.Ok(new { requeued = true, topic = targetTopic, partition = request.Partition, offset = request.Offset });
});

app.Run();

public sealed record DlqRequeueRequest(int Partition, long Offset, string? TargetTopic);

public interface IDlqReader
{
    Task<IReadOnlyList<DlqMessage>> ReadLatestAsync(int limit, CancellationToken ct);
    Task<DlqMessage?> ReadByOffsetAsync(int partition, long offset, CancellationToken ct);
}

public sealed class DlqReader : IDlqReader
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DlqReader> _logger;

    public DlqReader(IConfiguration config, IHostEnvironment env, ILogger<DlqReader> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DlqMessage>> ReadLatestAsync(int limit, CancellationToken ct)
    {
        var brokers = ConfigurationGuard.GetRequired(_config, _env, "Kafka:Brokers", "localhost:29092");
        var dlqTopic = _config["Kafka:DLQTopic"] ?? "dlq.events";
        var groupId = $"dlq-reader-{Guid.NewGuid():N}";

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnablePartitionEof = true
        }).Build();

        using var adminClient = new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers = brokers
                })
            .Build();
        
        var metadata = adminClient.GetMetadata(dlqTopic, TimeSpan.FromSeconds(5));
        var partitions = metadata.Topics.FirstOrDefault()?.Partitions
            .Select(p => new TopicPartition(dlqTopic, p.PartitionId))
            .ToArray() ?? Array.Empty<TopicPartition>();

        if (partitions.Length == 0)
            return Array.Empty<DlqMessage>();

        var perPartition = Math.Max(1, (int)Math.Ceiling(limit / (double)partitions.Length));
        var endOffsets = new Dictionary<TopicPartition, long>();

        consumer.Assign(partitions);
        foreach (var tp in partitions)
        {
            var watermarks = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5));
            var start = Math.Max(watermarks.Low.Value, watermarks.High.Value - perPartition);
            endOffsets[tp] = watermarks.High.Value;
            consumer.Seek(new TopicPartitionOffset(tp, start));
        }

        var items = new List<DlqMessage>();
        var done = new HashSet<TopicPartition>();

        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline && done.Count < partitions.Length)
        {
            ct.ThrowIfCancellationRequested();
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(200));
            if (cr == null)
                continue;

            if (endOffsets.TryGetValue(cr.TopicPartition, out var end) && cr.Offset.Value >= end)
            {
                done.Add(cr.TopicPartition);
                continue;
            }

            items.Add(ToMessage(cr));
            if (items.Count >= limit)
                break;
        }

        return items
            .OrderByDescending(i => i.TimestampUtc)
            .Take(limit)
            .ToArray();
    }

    public Task<DlqMessage?> ReadByOffsetAsync(int partition, long offset, CancellationToken ct)
    {
        var brokers = ConfigurationGuard.GetRequired(_config, _env, "Kafka:Brokers", "localhost:29092");
        var dlqTopic = _config["Kafka:DLQTopic"] ?? "dlq.events";
        var groupId = $"dlq-reader-{Guid.NewGuid():N}";

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        var tp = new TopicPartition(dlqTopic, new Partition(partition));
        consumer.Assign(tp);
        consumer.Seek(new TopicPartitionOffset(tp, new Offset(offset)));

        var cr = consumer.Consume(TimeSpan.FromSeconds(2));
        if (cr == null || cr.Offset.Value != offset)
            return Task.FromResult<DlqMessage?>(null);

        return Task.FromResult<DlqMessage?>(ToMessage(cr));
    }

    private DlqMessage ToMessage(ConsumeResult<string, string> cr)
    {
        var headers = new Dictionary<string, string>();
        foreach (var h in cr.Message.Headers ?? new Headers())
        {
            var value = h.GetValueBytes() is { Length: > 0 } b ? Encoding.UTF8.GetString(b) : string.Empty;
            headers[h.Key] = value;
        }

        headers.TryGetValue("original-topic", out var originalTopic);
        headers.TryGetValue("event-id", out var eventId);

        return new DlqMessage(
            cr.Topic,
            cr.Partition.Value,
            cr.Offset.Value,
            cr.Message.Key,
            cr.Message.Value ?? string.Empty,
            cr.Message.Timestamp.UtcDateTime,
            originalTopic,
            eventId,
            headers);
    }
}

public sealed record DlqMessage(
    string Topic,
    int Partition,
    long Offset,
    string? Key,
    string Payload,
    DateTime TimestampUtc,
    string? OriginalTopic,
    string? EventId,
    IReadOnlyDictionary<string, string> Headers);
