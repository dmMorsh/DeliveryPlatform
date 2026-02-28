using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Интерфейс для Kafka consumer
/// </summary>
public interface IEventConsumer
{
    /// <summary>Начать слушать события</summary>
    Task StartConsumingAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Реализация Kafka consumer с обработчиками событий
/// </summary>
public abstract class KafkaEventConsumerBase : IEventConsumer
{
    private static readonly Meter Meter = new("Shared.KafkaConsumer", "1.0.0");
    private static readonly Counter<long> EventsReceived = Meter.CreateCounter<long>("kafka_events_received_total");
    private static readonly Counter<long> EventsHandled = Meter.CreateCounter<long>("kafka_events_handled_total");
    private static readonly Counter<long> EventsFailed = Meter.CreateCounter<long>("kafka_events_failed_total");
    private static readonly Counter<long> EventsDlq = Meter.CreateCounter<long>("kafka_events_dlq_total");
    private static readonly Counter<long> EventsRetryScheduled = Meter.CreateCounter<long>("kafka_events_retry_scheduled_total");
    private static readonly Counter<long> EventsInboxSkipped = Meter.CreateCounter<long>("kafka_events_inbox_skipped_total");
    private static readonly Counter<long> EventsPoisoned = Meter.CreateCounter<long>("kafka_events_poisoned_total");
    private static readonly Counter<long> BackpressurePauses = Meter.CreateCounter<long>("kafka_backpressure_pause_total");

    protected readonly IConsumer<string, string> _consumer;
    protected readonly ILogger _logger;
    protected readonly string[] _topics;
    protected readonly IServiceScopeFactory _scopeFactory;
    protected readonly IEventProducer? _dlqProducer;
    protected readonly string _dlqTopic;
    protected readonly string _retryTopic;
    protected readonly int _retryMaxAttempts;
    private readonly int _retryMaxAgeSeconds;
    protected readonly HashSet<string> _poisonEventTypes;
    private readonly int _backpressureMaxLag;
    private readonly int _backpressureDelayMs;

    public KafkaEventConsumerBase(
        IConfiguration config,
        IHostEnvironment env,
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer? dlqProducer = null,
        string? dlqTopic = null,
        string? groupIdOverride = null,
        params string[] topics)
    {
        _logger = logger;
        _topics = topics;
        _scopeFactory = scopeFactory;
        _dlqProducer = dlqProducer;
        _dlqTopic = dlqTopic ?? (config["Kafka:DLQTopic"] ?? "dlq.events");
        _retryTopic = ConfigurationGuard.GetRequired(config, env, "Kafka:Retry:Topic", "retry.events");
        _retryMaxAttempts = int.TryParse(config["Kafka:Retry:MaxAttempts"], out var maxAttempts) ? maxAttempts : 3;
        _retryMaxAgeSeconds = int.TryParse(config["Kafka:Retry:MaxAgeSeconds"], out var maxAgeSeconds) ? maxAgeSeconds : 3600;
        _backpressureMaxLag = int.TryParse(config["Kafka:Backpressure:MaxLag"], out var maxLag) ? maxLag : 0;
        _backpressureDelayMs = int.TryParse(config["Kafka:Backpressure:DelayMs"], out var delayMs) ? delayMs : 0;
        _poisonEventTypes = new HashSet<string>(
            (config.GetSection("Kafka:PoisonEventTypes").Get<string[]>() ??
             (config["Kafka:PoisonEventTypes"] ?? string.Empty)
                 .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var brokers = ConfigurationGuard.GetRequired(config, env, "Kafka:Brokers", "localhost:29092");
        var groupId = groupIdOverride
                      ?? ConfigurationGuard.GetRequired(config, env, "Kafka:GroupId", "default-group");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false,
            MaxPollIntervalMs = 300000, // 5 minutes
            SessionTimeoutMs = 30000,    // 30 seconds
            //ClientRack = ""// привязка 
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka error: {Error}", e.Reason);
            })
            .SetLogHandler((_, logMessage) =>
            {
                if (logMessage.Level >= SyslogLevel.Warning)
                    _logger.LogWarning("Kafka: {Message}", logMessage.Message);
            })
            .Build();

        _logger.LogInformation("KafkaEventConsumer initialized. Brokers: {Brokers}, GroupId: {GroupId}, Topics: {Topics}",
            brokers, groupId, string.Join(", ", topics));
    }

    /// <summary>
    /// Начать слушать события
    /// </summary>
    public virtual async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var topics = _topics;
            if (!string.IsNullOrWhiteSpace(_retryTopic) && !_topics.Contains(_retryTopic, StringComparer.OrdinalIgnoreCase))
                topics = _topics.Concat(new[] { _retryTopic }).ToArray();

            _consumer.Subscribe(topics);
            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", topics));

            await Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = _consumer.Consume(cancellationToken);
                        if (message == null) continue;
                        EventsReceived.Add(1, new KeyValuePair<string, object?>("topic", message.Topic));

                        _logger.LogInformation(
                            "Received message: Topic={Topic}, Partition={Partition}, Offset={Offset}, Key={Key}",
                            message.Topic,
                            message.Partition.Value,
                            message.Offset.Value,
                            message.Message.Key
                        );

                        // Получить тип события из headers
                        var eventType = GetHeaderValue(message.Message.Headers, "event-type");

                        if (eventType == null)
                        {
                            EventsDlq.Add(1, new KeyValuePair<string, object?>("reason", "missing_event_type"));
                            await PublishDlqAsync(message, "unknown", message.Message.Value ?? string.Empty, "missing_event_type", cancellationToken);
                            _consumer.Commit(message);
                            continue;
                        }

                        {
                            if (_poisonEventTypes.Contains(eventType))
                            {
                                EventsPoisoned.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
                                EventsDlq.Add(1, new KeyValuePair<string, object?>("reason", "poison_event"));
                                await PublishDlqAsync(message, eventType, message.Message.Value ?? string.Empty, "poison_event", cancellationToken);
                                _consumer.Commit(message);
                                continue;
                            }

                            var eventId = GetHeaderValue(message.Message.Headers, "event-id")
                                          ?? $"{message.Topic}:{message.Partition.Value}:{message.Offset.Value}";

                            var aggregateId = Guid.TryParse(message.Message.Key, out var aggId)
                                ? aggId
                                : Guid.Empty;
                            var correlationId = GetHeaderValue(message.Message.Headers, "X-Correlation-Id", "correlation-id", "x-correlation-id");

                            using var logScope = _logger.BeginScope(new Dictionary<string, object?>
                            {
                                ["event_id"] = eventId,
                                ["event_type"] = eventType,
                                ["correlation_id"] = correlationId ?? string.Empty,
                                ["topic"] = message.Topic,
                                ["partition"] = message.Partition.Value,
                                ["offset"] = message.Offset.Value,
                                ["aggregate_id"] = aggregateId == Guid.Empty ? string.Empty : aggregateId.ToString()
                            });

                            using var scope = _scopeFactory.CreateScope();
                            var inbox = scope.ServiceProvider.GetService<IEventInbox>();
                            if (inbox != null)
                            {
                                var started = await inbox.TryStartAsync(
                                    eventId,
                                    eventType,
                                    aggregateId,
                                    message.Topic,
                                    message.Partition.Value,
                                    message.Offset.Value,
                                    cancellationToken);

                                if (!started)
                                {
                                    EventsInboxSkipped.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
                                    _consumer.Commit(message);
                                    continue;
                                }
                            }

                            var json = message.Message.Value;
                            var handled = false;
                            try
                            {
                                var sw = Stopwatch.StartNew();
                                handled = await HandleMessageAsync(eventType, json, message);
                                sw.Stop();
                                if (handled)
                                    _logger.LogDebug("Handled event {EventType} in {ElapsedMs}ms", eventType, sw.ElapsedMilliseconds);
                            }
                            catch (NonRetryableException ex)
                            {
                                _logger.LogError(ex, "Non-retryable event error: {EventType}", eventType);
                                if (inbox != null)
                                    await inbox.MarkFailedAsync(eventId, "non_retriable", cancellationToken);

                                EventsDlq.Add(1, new KeyValuePair<string, object?>("reason", "non_retriable_exception"));
                                await PublishDlqAsync(message, eventType, json ?? string.Empty, "non_retriable_exception", cancellationToken);
                                _consumer.Commit(message);
                                continue;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Unhandled error in handler for event {EventType}", eventType);
                                handled = false;
                            }

                            if (inbox != null)
                            {
                                if (handled)
                                    await inbox.MarkProcessedAsync(eventId, cancellationToken);
                                else
                                    await inbox.MarkFailedAsync(eventId, "handler_failed", cancellationToken);
                            }

                        if (handled)
                            EventsHandled.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
                        else
                        {
                            EventsFailed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
                            await HandleFailureAsync(message, eventType, json ?? string.Empty, eventId, cancellationToken);
                        }
                        }

                        _consumer.Commit(message);

                        if (_backpressureMaxLag > 0 && _backpressureDelayMs > 0)
                            await ApplyBackpressureIfNeededAsync(message, cancellationToken);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                    }
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in consumer");
            throw;
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }
    }

    private async Task ApplyBackpressureIfNeededAsync(ConsumeResult<string, string> message, CancellationToken ct)
    {
        try
        {
            var tp = new TopicPartition(message.Topic, message.Partition);
            var watermarks = _consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(1));
            var lag = watermarks.High.Value - message.Offset.Value - 1;
            if (lag <= _backpressureMaxLag)
                return;

            _logger.LogWarning(
                "Backpressure activated. Topic={Topic} Partition={Partition} Lag={Lag} MaxLag={MaxLag}. Pausing for {DelayMs}ms",
                message.Topic,
                message.Partition.Value,
                lag,
                _backpressureMaxLag,
                _backpressureDelayMs);

            BackpressurePauses.Add(1, new KeyValuePair<string, object?>("topic", message.Topic));
            _consumer.Pause(new[] { tp });
            await Task.Delay(_backpressureDelayMs, ct);
            _consumer.Resume(new[] { tp });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backpressure check failed");
        }
    }

    /// <summary>
    /// Переопределить для обработки сообщений
    /// </summary>
    protected abstract Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message);

    private async Task HandleFailureAsync(
        ConsumeResult<string, string> message,
        string eventType,
        string json,
        string eventId,
        CancellationToken ct)
    {
        if (_retryMaxAgeSeconds > 0 && message.Message.Timestamp.Type != TimestampType.NotAvailable)
        {
            var age = DateTimeOffset.UtcNow - message.Message.Timestamp.UtcDateTime;
            if (age > TimeSpan.FromSeconds(_retryMaxAgeSeconds))
            {
                EventsDlq.Add(1, new KeyValuePair<string, object?>("reason", "retry_expired"));
                await PublishDlqAsync(message, eventType, json, "retry_expired", ct);
                return;
            }
        }

        var retryCount = message.Message.Headers
            .FirstOrDefault(h => h.Key == "retry-count")
            ?.GetValueBytes() is { } bytes
                ? int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var val) ? val : 0
                : 0;

        if (_retryMaxAttempts > 0 && retryCount < _retryMaxAttempts && _dlqProducer != null)
        {
            var headers = BuildHeaders(message, eventId, eventType, "retry_scheduled");
            headers["retry-count"] = (retryCount + 1).ToString();
            EventsRetryScheduled.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
            await _dlqProducer.PublishAsync(
                _retryTopic,
                message.Message.Key ?? string.Empty,
                json,
                headers,
                ct);
            return;
        }

        EventsDlq.Add(1, new KeyValuePair<string, object?>("reason", "retry_exhausted"));
        await PublishDlqAsync(message, eventType, json, "retry_exhausted", ct);
    }

    private async Task PublishDlqAsync(
        ConsumeResult<string, string> message,
        string eventType,
        string json,
        string reason,
        CancellationToken ct)
    {
        if (_dlqProducer == null)
            return;

        var eventId = message.Message.Headers
            .FirstOrDefault(h => h.Key == "event-id")
            ?.GetValueBytes() is { } idBytes
                ? System.Text.Encoding.UTF8.GetString(idBytes)
                : $"{message.Topic}:{message.Partition.Value}:{message.Offset.Value}";

        var headers = BuildHeaders(message, eventId, eventType, reason);
        await _dlqProducer.PublishAsync(
            _dlqTopic,
            message.Message.Key ?? string.Empty,
            json,
            headers,
            ct);
    }

    private static string? GetHeaderValue(Headers? headers, params string[] keys)
    {
        if (headers == null || keys.Length == 0)
            return null;

        foreach (var key in keys)
        {
            foreach (var header in headers)
            {
                if (!string.Equals(header.Key, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                var valueBytes = header.GetValueBytes();
                return valueBytes is { Length: > 0 }
                    ? System.Text.Encoding.UTF8.GetString(valueBytes)
                    : string.Empty;
            }
        }

        return null;
    }

    private static Dictionary<string, string> BuildHeaders(
        ConsumeResult<string, string> message,
        string eventId,
        string eventType,
        string reason)
    {
        var headers = new Dictionary<string, string>
        {
            ["event-id"] = eventId,
            ["event-type"] = eventType,
            ["original-topic"] = message.Topic,
            ["original-partition"] = message.Partition.Value.ToString(),
            ["original-offset"] = message.Offset.Value.ToString(),
            ["failure-reason"] = reason
        };

        foreach (var h in message.Message.Headers ?? new Headers())
        {
            if (h.Key is "event-id" or "event-type" or "original-topic" or "original-partition" or "original-offset" or "failure-reason")
                continue;

            var value = h.GetValueBytes() is { Length: > 0 } b ? System.Text.Encoding.UTF8.GetString(b) : string.Empty;
            headers[h.Key] = value;
        }

        return headers;
    }
}
