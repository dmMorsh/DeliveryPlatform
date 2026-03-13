using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace CartService.Application.Services;
// TODO удалить?
/// <summary>
/// Обработчик событий для CartService
/// Слушает: пока ничего, просто что бы не забыть
/// </summary>
public class CartEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<CartEventConsumer> _logger;

    public CartEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<CartEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, null,
            "")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CartService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
            return false;
        }

        return true;
    }
}
