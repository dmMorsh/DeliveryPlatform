using System.Text.Json;
using Confluent.Kafka;
using Shared.Contracts.Events;
using Shared.Services;

namespace NotificationService.Services;

/// <summary>
/// Обработчик событий уведомлений
/// </summary>
public class NotificationEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<NotificationEventConsumer> _logger;
    private readonly INotificationService _notificationService;

    public NotificationEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<NotificationEventConsumer> logger,
        INotificationService notificationService,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, null,
            "order.events", "courier.events")
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Обработка входящих сообщений (async)
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("Processing event: {EventType}", eventType);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreatedAsync(json);
                    break;
                case "order.assigned":
                    await HandleOrderAssignedAsync(json);
                    break;
                case "order.status.changed":
                    await HandleOrderStatusChangedAsync(json);
                    break;
                case "order.delivered":
                    await HandleOrderDeliveredAsync(json);
                    break;
                case "courier.status.changed":
                    await HandleCourierStatusChangedAsync(json);
                    break;
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

    private async Task HandleOrderCreatedAsync(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) throw new NonRetryableException("Invalid OrderCreatedEvent payload");

            // Отправить уведомление клиенту
            _logger.LogInformation("Order created notification: Order {OrderNumber} for client {ClientId}",
                @event.OrderNumber, @event.ClientId);

            await _notificationService.SendNotificationAsync($"Your order {@event.OrderNumber} has been created");
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }

    private async Task HandleOrderAssignedAsync(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderAssignedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) throw new NonRetryableException("Invalid OrderAssignedEvent payload");

            _logger.LogInformation("Order assigned notification: Order {OrderId} to courier {CourierId}",
                @event.OrderId, @event.CourierId);

            await _notificationService.SendNotificationAsync($"Your order has been assigned to courier {@event.CourierName}");
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderAssignedEvent");
        }
    }

    private async Task HandleOrderStatusChangedAsync(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) throw new NonRetryableException("Invalid OrderStatusChangedEvent payload");

            _logger.LogInformation("Order status changed notification: Order {OrderId} status changed",
                @event.OrderId);

            await _notificationService.SendNotificationAsync($"Order status changed: {@event.NewStatus}");
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderStatusChangedEvent");
        }
    }

    private async Task HandleOrderDeliveredAsync(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderDeliveredEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) throw new NonRetryableException("Invalid OrderDeliveredEvent payload");

            _logger.LogInformation("Order delivered notification: Order {OrderId} delivered by courier {CourierId}",
                @event.OrderId, @event.CourierId);

            await _notificationService.SendNotificationAsync("Your order has been delivered!");
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderDeliveredEvent");
        }
    }

    private async Task HandleCourierStatusChangedAsync(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<CourierStatusChangedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) throw new NonRetryableException("Invalid CourierStatusChangedEvent payload");

            _logger.LogInformation("Courier status changed: Courier {CourierId} status changed",
                @event.CourierId);

            await _notificationService.SendNotificationAsync($"Courier status changed: {@event.CourierId}");
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CourierStatusChangedEvent");
        }
    }
}

/// <summary>
/// Интерфейс для отправки уведомлений
/// </summary>
public interface INotificationService
{
    Task SendNotificationAsync(string message);
}

/// <summary>
/// Mock реализация сервиса уведомлений
/// </summary>
public class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendNotificationAsync(string message)
    {
        _logger.LogInformation("NOTIFICATION: {Message}", message);
        return Task.CompletedTask;
    }
}
