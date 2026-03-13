using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;
using CatalogService.Application.Interfaces;

namespace CatalogService.Application.Services;

/// <summary>
/// Обработчик событий для CatalogService
/// Слушает: order.created (для обновления популярности), stock.reserved
/// </summary>
public class CatalogEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<CatalogEventConsumer> _logger;
    private readonly ICatalogMetricsStore _metrics;

    public CatalogEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<CatalogEventConsumer> logger,
        ICatalogMetricsStore metrics,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null,null,
            "order.events", "inventory.events")
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService и InventoryService
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CatalogService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
                    break;
                case "stock.reserved":
                    await HandleStockReserved(json);
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

    private async Task HandleOrderCreated(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderCreatedEvent payload");

        _logger.LogInformation("📊 CatalogService: Order created. OrderId={OrderId}.",
            @event.AggregateId);

        if (@event.Items == null)
            return;

        foreach (var item in @event.Items)
            await _metrics.IncrementProductSalesAsync(item.ProductId, item.Quantity);
    }

    private async Task HandleStockReserved(string json)
    {
        var @event = JsonSerializer.Deserialize<StockReservedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid StockReservedEvent payload");

        _logger.LogInformation("📦 CatalogService: Stock reserved. OrderId={OrderId}.",
            @event.OrderId);

        foreach (var item in @event.Items)
            await _metrics.IncrementReservedQuantityAsync(item.ProductId, item.Quantity);
    }
}
