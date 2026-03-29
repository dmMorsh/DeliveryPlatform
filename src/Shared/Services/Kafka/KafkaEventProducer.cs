using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Kafka producer for publishing domain events
/// </summary>
public interface IEventProducer
{
    /// <summary>Publish an event to Kafka</summary>
    Task PublishAsync(
        string topic,
        string key,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default);
}

/// <summary>
/// Kafka producer implementation
/// </summary>
public class KafkaEventProducer : IEventProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    private readonly string _defaultTopic;

    public KafkaEventProducer(IConfiguration config, IHostEnvironment env, ILogger<KafkaEventProducer> logger)
    {
        _logger = logger;
        
        var brokers = ConfigurationGuard.GetRequired(config, env, "Kafka:Brokers", "localhost:29092");
        _defaultTopic = ConfigurationGuard.GetRequired(config, env, "Kafka:DefaultTopic", "events");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = brokers,
            Acks = Acks.Leader, // Wait for leader and all replicas
            CompressionType = CompressionType.Snappy,
            MessageMaxBytes = 1000000, // 1MB
            LingerMs = 100, // Batch messages for 100ms for better throughput
            
            //EnableIdempotence = true,  // Prevent duplicate messages // Acks = Acks.All
            MessageSendMaxRetries = 3,  // Number of retries
            RetryBackoffMs = 1000, 
            Partitioner = Partitioner.Murmur2,
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
    /// Publish an event to Kafka
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

        _logger.LogDebug(
            "Kafka published: topic={Topic} partition={Partition} offset={Offset}",
            result.Topic,
            result.Partition,
            result.Offset);
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
