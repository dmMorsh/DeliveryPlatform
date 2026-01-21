using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Shared.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Kafka producer для публикации domain events
/// </summary>
public interface IEventProducer
{
    /// <summary>Опубликовать событие в Kafka</summary>
    Task PublishAsync(
        string topic,
        string key,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default);
    // Task PublishAsync<TEvent>(TEvent @event, string? topic = null) where TEvent : IntegrationEvent;
}

/// <summary>
/// Реализация Kafka producer
/// </summary>
public class KafkaEventProducer : IEventProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    private readonly string _defaultTopic;

    public KafkaEventProducer(IConfiguration config, ILogger<KafkaEventProducer> logger)
    {
        _logger = logger;
        
        var brokers = config["Kafka:Brokers"] ?? "localhost:29092";
        _defaultTopic = config["Kafka:DefaultTopic"] ?? "events";

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = brokers,
            Acks = Acks.All, // Wait for leader and all replicas
            CompressionType = CompressionType.Snappy,
            MessageMaxBytes = 1000000, // 1MB
            LingerMs = 100, // Batch messages for 100ms for better throughput
            
            EnableIdempotence = true,  // Предотвращение дублирования сообщений
            MessageSendMaxRetries = 3,  // Число повторных попыток
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(producerConfig)
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

        _logger.LogInformation("KafkaEventProducer initialized. Brokers: {Brokers}, Topic: {Topic}", brokers, _defaultTopic);
    }

    /// <summary>
    /// Опубликовать событие в Kafka
    /// </summary>
    ///
    public async Task PublishAsync(string topic, string key, string payload, IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        var message = new Message<string, string>
        {
            Key = key,
            Value = payload,
            Headers = new Headers()
        };

        foreach (var h in headers)
            message.Headers.Add(h.Key, Encoding.UTF8.GetBytes(h.Value));

        var result = await _producer.ProduceAsync(
            topic ?? _defaultTopic,
            message,
            ct);

        _logger.LogInformation(
            "Kafka published: topic={Topic} partition={Partition} offset={Offset}",
            result.Topic,
            result.Partition,
            result.Offset);
    }
    
    public async Task _PublishAsync<TEvent>(TEvent @event, string? topic = null) where TEvent : IntegrationEvent
    {
        try
        {
            var topicName = topic ?? _defaultTopic;
            var key = @event.AggregateId.ToString();
            var value = JsonSerializer.Serialize(@event, new JsonSerializerOptions { WriteIndented = false });

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(@event.EventType) },
                    { "aggregate-type", Encoding.UTF8.GetBytes(@event.AggregateType) },
                    { "timestamp", Encoding.UTF8.GetBytes(@event.Timestamp.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(topicName, message);

            _logger.LogInformation(
                "Event published: Type={EventType}, AggregateId={AggregateId}, Topic={Topic}, Partition={Partition}, Offset={Offset}",
                @event.EventType,
                @event.AggregateId,
                topicName,
                result.Partition.Value,
                result.Offset.Value
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to Kafka", @event.EventType);
            throw;
        }
    }

    /// <summary>
    /// Graceful shutdown
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _producer?.Flush();
        _producer?.Dispose();
        await Task.CompletedTask;
    }
}
