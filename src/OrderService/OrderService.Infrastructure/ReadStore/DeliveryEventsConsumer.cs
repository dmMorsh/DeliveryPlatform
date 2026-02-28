using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace OrderService.Infrastructure.ReadStore;

/// <summary>
/// Потребитель событий доставки (DeliveryService) для обновления OrderRead проекции с ETA и деталями доставки
/// </summary>
public sealed class DeliveryEventsConsumer : KafkaEventConsumerBase
{
    private readonly ILogger<DeliveryEventsConsumer> _logger;

    public DeliveryEventsConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DeliveryEventsConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "delivery.events")
    {
        _logger = logger;
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var projector = scope.ServiceProvider.GetRequiredService<IOrderReadProjector>();
            var db = scope.ServiceProvider.GetRequiredService<OrderReadDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync();
            switch (eventType)
            {
                case "delivery.assigned":
                    var assigned = Deserialize<DeliveryAssignedEvent>(json);
                    if (assigned == null) return true;
                    await projector.HandleAsync(assigned, CancellationToken.None);
                    break;
                case "delivery.picked_up":
                case "delivery.in_transit":
                case "delivery.delivered":
                case "delivery.cancelled":
                case "delivery.failed":
                    // Другие события доставки — можно добавить позже при необходимости
                    return true;
                default:
                    return true;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delivery events read projection error for event {EventType}", eventType);
            return false;
        }
    }

    private static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
