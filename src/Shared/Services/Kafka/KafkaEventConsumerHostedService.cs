using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

public sealed class KafkaEventConsumerHostedService<TConsumer> : BackgroundService where TConsumer : class, IEventConsumer
{
    private readonly TConsumer _consumer;
    private readonly ILogger<KafkaEventConsumerHostedService<TConsumer>> _logger;

    public KafkaEventConsumerHostedService(
        TConsumer consumer,
        ILogger<KafkaEventConsumerHostedService<TConsumer>> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumer: {Consumer}", typeof(TConsumer).Name);
        try
        {
            await _consumer.StartConsumingAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Kafka consumer stopped: {Consumer}", typeof(TConsumer).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka consumer failed: {Consumer}", typeof(TConsumer).Name);
            throw;
        }
    }
}
