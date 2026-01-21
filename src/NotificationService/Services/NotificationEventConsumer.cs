using System.Text.Json;
using Confluent.Kafka;
using Shared.Services;
using Shared.Contracts.Events;

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
        ILogger<NotificationEventConsumer> logger,
        INotificationService notificationService)
        : base(config, logger, "order.events", "courier.events")
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Обработка входящих сообщений (async)
    /// </summary>
    protected override async Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("Processing event: {EventType}", eventType);

            switch (eventType)
            {
                case "order.created":
                    await Task.Run(() => HandleOrderCreated(json));
                    break;
                case "order.assigned":
                    await Task.Run(() => HandleOrderAssigned(json));
                    break;
                case "order.status.changed":
                    await Task.Run(() => HandleOrderStatusChanged(json));
                    break;
                case "order.delivered":
                    await Task.Run(() => HandleOrderDelivered(json));
                    break;
                case "courier.status.changed":
                    await Task.Run(() => HandleCourierStatusChanged(json));
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

    private void HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            // Отправить уведомление клиенту
            _logger.LogInformation("Order created notification: Order {OrderNumber} for client {ClientId}",
                @event.OrderNumber, @event.ClientId);

                // TODO: Отправить SMS/Email уведомление
                _notificationService.SendNotificationAsync($"Your order {@event.OrderNumber} has been created").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }

    private void HandleOrderAssigned(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderAssignedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Order assigned notification: Order {OrderId} to courier {CourierId}",
                @event.OrderId, @event.CourierId);

                // TODO: Отправить SMS/Email уведомление
                _notificationService.SendNotificationAsync($"Your order has been assigned to courier {@event.CourierName}").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderAssignedEvent");
        }
    }

    private void HandleOrderStatusChanged(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Order status changed notification: Order {OrderId} status changed",
                @event.OrderId);

                // TODO: Отправить SMS/Email уведомление
                _notificationService.SendNotificationAsync($"Order status changed: {@event.NewStatus}").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderStatusChangedEvent");
        }
    }

    private void HandleOrderDelivered(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderDeliveredEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Order delivered notification: Order {OrderId} delivered by courier {CourierId}",
                @event.OrderId, @event.CourierId);

                // TODO: Отправить SMS/Email уведомление
                _notificationService.SendNotificationAsync($"Your order has been delivered!").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderDeliveredEvent");
        }
    }

    private void HandleCourierStatusChanged(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<CourierStatusChangedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Courier status changed: Courier {CourierId} status changed",
                @event.CourierId);

            // TODO: Логирование статуса курьера
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
        // TODO: Реальная реализация отправки SMS/Email/Push
        return Task.CompletedTask;
    }
}
