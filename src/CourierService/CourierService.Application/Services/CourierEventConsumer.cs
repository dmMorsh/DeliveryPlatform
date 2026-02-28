using System.Text.Json;
using Confluent.Kafka;
using CourierService.Application.Commands.UpdateCourierStatus;
using CourierService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;
using Shared.Utilities;

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
        IHostEnvironment env,
        ILogger<CourierEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, null, "order.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CourierService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.assigned":
                    await HandleOrderAssigned(json);
                    break;
                case "order.canceled":
                    await HandleOrderCanceled(json);
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

    private async Task HandleOrderAssigned(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderAssignedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderAssignedEvent payload");

        _logger.LogInformation("📍 CourierService: Order assigned to courier. OrderId={OrderId}, CourierId={CourierId}. " +
            "Updating courier status to OnDelivery",
            @event.OrderId, @event.CourierId);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateCourierStatusCommand(
            @event.CourierId,
            (int)CourierStatus.OnDelivery,
            null,
            null,
            null));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }

    private async Task HandleOrderCanceled(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderCanceledEvent payload");
        if (@event.CourierId == Guid.Empty) return;

        _logger.LogInformation("📍 CourierService: Order canceled. OrderId={OrderId}, CourierId={CourierId}. " +
            "Updating courier status to Online",
            @event.OrderId, @event.CourierId);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateCourierStatusCommand(
            @event.CourierId,
            (int)CourierStatus.Online,
            null,
            null,
            null));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }
}
