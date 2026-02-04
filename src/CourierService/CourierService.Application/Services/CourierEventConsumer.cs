using System.Text.Json;
using Confluent.Kafka;
using Shared.Services;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CourierService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для CourierService
/// Слушает: order.assigned (для получения информации о заказе)
/// </summary>
public class CourierEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<CourierEventConsumer> _logger;

    public CourierEventConsumer(
        IConfiguration config,
        ILogger<CourierEventConsumer> logger)
        : base(config, logger, "order.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService
    /// </summary>
    protected override async Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CourierService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.assigned":
                    await HandleOrderAssigned(json);
                    break;
                case "order.created":
                    await HandleOrderCreated(json);
                    break;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
        }
    }

    private async Task HandleOrderAssigned(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderAssignedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📍 CourierService: Order assigned to courier. OrderId={OrderId}, CourierId={CourierId}. " +
                "🚚 TODO: Notify courier about new delivery",
                @event.OrderId, @event.CourierId);
            
            // TODO: Implement courier notification
            // This would typically:
            // 1. Get courier details
            // 2. Get order details (via gRPC from OrderService)
            // 3. Send push notification to courier mobile app
            // 4. Add delivery to courier's task list
            // 5. Update delivery status
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderAssignedEvent");
        }
    }

    private async Task HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 CourierService: Order created. OrderId={OrderId}. " +
                "📊 TODO: Update metrics or prepare for assignment",
                @event.AggregateId);
            
            // TODO: Handle order creation
            // Could update demand map, prepare for auto-assignment, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }
}
