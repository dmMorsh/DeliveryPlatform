using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace OrderService.Infrastructure.ReadStore;

public sealed class OrderReadProjectionConsumer : KafkaEventConsumerBase
{
    private readonly ILogger<OrderReadProjectionConsumer> _logger;

    public OrderReadProjectionConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<OrderReadProjectionConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "order.events")
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
                case "order.created":
                    var created = Deserialize<OrderCreatedEvent>(json);
                    if (created == null) return true;
                    await projector.HandleAsync(created, CancellationToken.None);
                    break;
                case "order.status.changed":
                    var statusChanged = Deserialize<OrderStatusChangedEvent>(json);
                    if (statusChanged == null) return true;
                    await projector.HandleAsync(statusChanged, CancellationToken.None);
                    break;
                case "order.ready":
                    var ready = Deserialize<OrderReadyEvent>(json);
                    if (ready == null) return true;
                    await projector.HandleAsync(ready, CancellationToken.None);
                    break;
                case "order.accepted":
                    var accepted = Deserialize<OrderAcceptedEvent>(json);
                    if (accepted == null) return true;
                    await projector.HandleAsync(accepted, CancellationToken.None);
                    break;
                case "order.rejected":
                    var rejected = Deserialize<OrderRejectedEvent>(json);
                    if (rejected == null) return true;
                    await projector.HandleAsync(rejected, CancellationToken.None);
                    break;
                case "order.canceled":
                    var canceled = Deserialize<OrderCanceledEvent>(json);
                    if (canceled == null) return true;
                    await projector.HandleAsync(canceled, CancellationToken.None);
                    break;
                case "order.assigned":
                    var assigned = Deserialize<OrderAssignedEvent>(json);
                    if (assigned == null) return true;
                    await projector.HandleAsync(assigned, CancellationToken.None);
                    break;
                case "order.delivered":
                    var delivered = Deserialize<OrderDeliveredEvent>(json);
                    if (delivered == null) return true;
                    await projector.HandleAsync(delivered, CancellationToken.None);
                    break;
                case "order.kitchen_delayed":
                    var delayed = Deserialize<OrderKitchenDelayedEvent>(json);
                    if (delayed == null) return true;
                    await projector.HandleAsync(delayed, CancellationToken.None);
                    break;
                default:
                    return true;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Read projection error for event {EventType}", eventType);
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
