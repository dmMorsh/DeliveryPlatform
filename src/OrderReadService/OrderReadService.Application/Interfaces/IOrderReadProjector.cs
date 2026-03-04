using Shared.Contracts.Events;

namespace OrderReadService.Application.Interfaces;

public interface IOrderReadProjector
{
    Task HandleAsync(OrderCreatedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderStatusChangedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderReadyEvent evt, CancellationToken ct);
    Task HandleAsync(OrderAcceptedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderRejectedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderCanceledEvent evt, CancellationToken ct);
    Task HandleAsync(OrderKitchenDelayedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderAssignedEvent evt, CancellationToken ct);
    Task HandleAsync(OrderDeliveredEvent evt, CancellationToken ct);
}
