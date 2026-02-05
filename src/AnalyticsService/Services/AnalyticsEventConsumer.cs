using System.Text.Json;
using Confluent.Kafka;
using Shared.Contracts.Events;
using Shared.Services;

namespace AnalyticsService.Services;

/// <summary>
/// Сборщик аналитики из событий
/// </summary>
public class AnalyticsEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<AnalyticsEventConsumer> _logger;

    // In-memory статистика
    private readonly object _lockObj = new();
    private int _totalOrders = 0;
    private int _deliveredOrders = 0;
    private int _totalCouriers = 0;
    private Dictionary<int, int> _ordersByStatus = new();
    private Dictionary<Guid, int> _deliveriesByCourier = new();

    public AnalyticsEventConsumer(
        IConfiguration config,
        ILogger<AnalyticsEventConsumer> logger)
        : base(config, logger, "cart.events", "order.events", "courier.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих сообщений (async)
    /// </summary>
    protected override Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            lock (_lockObj)
            {
                switch (eventType)
                {
                    case "order.created":
                        HandleOrderCreated(json);
                        break;
                    case "order.assigned":
                        HandleOrderAssigned(json);
                        break;
                    case "order.status.changed":
                        HandleOrderStatusChanged(json);
                        break;
                    case "order.delivered":
                        HandleOrderDelivered(json);
                        break;
                    case "courier.status.changed":
                        HandleCourierStatusChanged(json);
                        break;
                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventType);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
        }

        return Task.CompletedTask;
    }

    private void HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _totalOrders++;
            if (!_ordersByStatus.ContainsKey(0))
                _ordersByStatus[0] = 0;
            _ordersByStatus[0]++;

            _logger.LogInformation("Analytics: Order created. Total orders: {Total}", _totalOrders);
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
            var @event = JsonSerializer.Deserialize<OrderAssignedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Analytics: Order {OrderId} assigned to courier {CourierId}", 
                @event.OrderId, @event.CourierId);
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
            var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            // Update status counters
            if (_ordersByStatus.ContainsKey(@event.PreviousStatus))
                _ordersByStatus[@event.PreviousStatus]--;
            if (!_ordersByStatus.ContainsKey(@event.NewStatus))
                _ordersByStatus[@event.NewStatus] = 0;
            _ordersByStatus[@event.NewStatus]++;

            _logger.LogInformation("Analytics: Order {OrderId} status changed: {Old} -> {New}", 
                @event.OrderId, @event.PreviousStatus, @event.NewStatus);
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
            var @event = JsonSerializer.Deserialize<OrderDeliveredEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _deliveredOrders++;
            if (!_deliveriesByCourier.ContainsKey(@event.CourierId))
                _deliveriesByCourier[@event.CourierId] = 0;
            _deliveriesByCourier[@event.CourierId]++;

            _logger.LogInformation("Analytics: Order {OrderId} delivered by courier {CourierId}. Total delivered: {Total}", 
                @event.OrderId, @event.CourierId, _deliveredOrders);
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
            var @event = JsonSerializer.Deserialize<CourierStatusChangedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("Analytics: Courier {CourierId} status changed: {Old} -> {New}", 
                @event.CourierId, @event.PreviousStatus, @event.NewStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CourierStatusChangedEvent");
        }
    }

    /// <summary>
    /// Получить текущую статистику (для /metrics endpoint)
    /// </summary>
    public AnalyticsSnapshot GetSnapshot()
    {
        lock (_lockObj)
        {
            return new AnalyticsSnapshot
            {
                TotalOrders = _totalOrders,
                DeliveredOrders = _deliveredOrders,
                TotalCouriers = _totalCouriers,
                OrdersByStatus = new Dictionary<int, int>(_ordersByStatus),
                DeliveriesByCourier = new Dictionary<Guid, int>(_deliveriesByCourier),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Снимок аналитики
/// </summary>
public class AnalyticsSnapshot
{
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int TotalCouriers { get; set; }
    public Dictionary<int, int> OrdersByStatus { get; set; } = new();
    public Dictionary<Guid, int> DeliveriesByCourier { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
