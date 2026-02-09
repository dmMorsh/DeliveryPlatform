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
    protected readonly IConsumer<string, string> _consumer;
    protected readonly ILogger _logger;
    protected readonly string[] _topics;
    protected readonly IServiceScopeFactory _scopeFactory;
    protected readonly IEventProducer? _dlqProducer;
    protected readonly string _dlqTopic;
    protected readonly string _retryTopic;
    protected readonly int _retryMaxAttempts;
    protected readonly HashSet<string> _poisonEventTypes;

    public KafkaEventConsumerBase(
        IConfiguration config,
        IHostEnvironment env,
        ILogger logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer? dlqProducer = null,
        string? dlqTopic = null,
        params string[] topics)
    {
        _logger = logger;
        _topics = topics;
        _scopeFactory = scopeFactory;
        _dlqProducer = dlqProducer;
        _dlqTopic = dlqTopic ?? (config["Kafka:DLQTopic"] ?? "dlq.events");
        _retryTopic = ConfigurationGuard.GetRequired(config, env, "Kafka:Retry:Topic", "retry.events");
        _retryMaxAttempts = int.TryParse(config["Kafka:Retry:MaxAttempts"], out var maxAttempts) ? maxAttempts : 3;
        _poisonEventTypes = new HashSet<string>(
            (config.GetSection("Kafka:PoisonEventTypes").Get<string[]>() ??
             (config["Kafka:PoisonEventTypes"] ?? string.Empty)
                 .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var brokers = ConfigurationGuard.GetRequired(config, env, "Kafka:Brokers", "localhost:29092");
        var groupId = ConfigurationGuard.GetRequired(config, env, "Kafka:GroupId", "default-group");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false,
            MaxPollIntervalMs = 300000, // 5 minutes
            SessionTimeoutMs = 30000,    // 30 seconds
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

                        _logger.LogInformation(
                            "Received message: Topic={Topic}, Partition={Partition}, Offset={Offset}, Key={Key}",
                            message.Topic,
                            message.Partition.Value,
                            message.Offset.Value,
                            message.Message.Key
                        );

                        // Получить тип события из headers
                        var eventType = message.Message.Headers
                            .FirstOrDefault(h => h.Key == "event-type")
                            ?.GetValueBytes() is { } bytes
                                ? System.Text.Encoding.UTF8.GetString(bytes)
                                : null;

                        if (eventType != null)
                        {
                            if (_poisonEventTypes.Contains(eventType))
                            {
                                await PublishDlqAsync(message, eventType, message.Message.Value ?? string.Empty, "poison_event", cancellationToken);
                                _consumer.Commit(message);
                                continue;
                            }

                            var eventId = message.Message.Headers
                                .FirstOrDefault(h => h.Key == "event-id")
                                ?.GetValueBytes() is { } idBytes
                                    ? System.Text.Encoding.UTF8.GetString(idBytes)
                                    : $"{message.Topic}:{message.Partition.Value}:{message.Offset.Value}";

                            var aggregateId = Guid.TryParse(message.Message.Key, out var aggId)
                                ? aggId
                                : Guid.Empty;

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
                                    _consumer.Commit(message);
                                    continue;
                                }
                            }

                            var json = message.Message.Value;
                            var handled = await HandleMessageAsync(eventType, json, message);

                            if (inbox != null)
                            {
                                if (handled)
                                    await inbox.MarkProcessedAsync(eventId, cancellationToken);
                                else
                                    await inbox.MarkFailedAsync(eventId, "handler_failed", cancellationToken);
                            }

                            if (!handled)
                                await HandleFailureAsync(message, eventType, json ?? string.Empty, eventId, cancellationToken);
                        }

                        _consumer.Commit(message);
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
        var retryCount = message.Message.Headers
            .FirstOrDefault(h => h.Key == "retry-count")
            ?.GetValueBytes() is { } bytes
                ? int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var val) ? val : 0
                : 0;

        if (_retryMaxAttempts > 0 && retryCount < _retryMaxAttempts && _dlqProducer != null)
        {
            var headers = BuildHeaders(message, eventId, eventType, "retry_scheduled");
            headers["retry-count"] = (retryCount + 1).ToString();
            await _dlqProducer.PublishAsync(
                _retryTopic,
                message.Message.Key ?? string.Empty,
                json,
                headers,
                ct);
            return;
        }

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
