using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Commands.MarkPaymentReady;
using PaymentService.Application.Commands.ProcessOrderCanceled;
using Shared.Contracts.Events;
using Shared.Services;
using Shared.Utilities;

namespace PaymentService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для PaymentService
/// Слушает: order.canceled
/// </summary>
public class PaymentEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<PaymentEventConsumer> _logger;

    private static class OrderStatusIds
    {
        public const int Reserved = (int)OrderStatusCode.Reserved;
        public const int Cancelled = (int)OrderStatusCode.Cancelled;
        public const int Failed = (int)OrderStatusCode.Failed;
    }

    public PaymentEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<PaymentEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, null,
            "order.events")
    {
        _logger = logger;
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("PaymentService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
                    return true;
                case "order.status.changed":
                    await HandleOrderStatusChanged(json);
                    return true;
                case "order.canceled":
                    await HandleOrderCanceled(json);
                    return true;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    return true;
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

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var currency = string.IsNullOrWhiteSpace(@event.Currency) ? "RUB" : @event.Currency;
        var cmd = new CreatePaymentCommand(@event.OrderId, @event.CostCents, currency);
        await mediator.Send(cmd);
    }

    private async Task HandleOrderCanceled(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderCanceledEvent payload");

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new ProcessOrderCanceledCommand(@event.OrderId));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }

    private async Task HandleOrderStatusChanged(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderStatusChangedEvent payload");

        if (@event.NewStatus == OrderStatusIds.Reserved)
        {
            using var serviceScope = _scopeFactory.CreateScope();
            var scopeMediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
            var apiResponse = await scopeMediator.Send(new MarkPaymentReadyCommand(@event.OrderId));
            if (!apiResponse.Success)
            {
                var message = apiResponse.Message ?? string.Join("; ", apiResponse.Errors ?? []);
                if (apiResponse.ErrorCode == ErrorCodes.NotFound)
                    throw new Exception(message);
                throw new NonRetryableException(message);
            }
            return;
        }

        if (@event.NewStatus is not (OrderStatusIds.Cancelled or OrderStatusIds.Failed))
            return;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new ProcessOrderCanceledCommand(@event.OrderId));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }
}
