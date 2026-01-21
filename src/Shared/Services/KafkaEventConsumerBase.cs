using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
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

    public KafkaEventConsumerBase(IConfiguration config, ILogger logger, params string[] topics)
    {
        _logger = logger;
        _topics = topics;

        var brokers = config["Kafka:Brokers"] ?? "localhost:29092";
        var groupId = config["Kafka:GroupId"] ?? "default-group";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = brokers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
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
            _consumer.Subscribe(_topics);
            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", _topics));

            await Task.Run(() =>
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
                                // Парсим JSON
                                var json = message.Message.Value;
                                // call async handler and don't block the consumer loop
                                _ = HandleMessageAsync(eventType, json, message);
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
        protected abstract Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message);
}
