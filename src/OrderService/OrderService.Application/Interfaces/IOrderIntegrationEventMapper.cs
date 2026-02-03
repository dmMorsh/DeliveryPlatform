using OrderService.Domain.Aggregates;
using OrderService.Domain.Events;
using Shared.Contracts.Events;

namespace OrderService.Application.Interfaces;

public interface IOrderIntegrationEventMapper
{
    OrderAssignedEvent MapOrderAssignedEvent(Order order, Guid courierId, string courierName, string? courierPhone = null);
    OrderStatusChangedEvent MapOrderStatusChangedEvent(Order order, int oldStatus, int newStatus);
    OrderDeliveredEvent MapOrderDeliveredEvent(Order order, Guid courierId);
    IntegrationEvent? MapFromDomainEvent(Domain.SeedWork.DomainEvent domainEvent);
    IntegrationEvent? MapFromOrderCreatedDomainEvent(OrderCreatedDomainEvent domainEvent,
        IEnumerable<DomainOrderItemSnapshot>? snapshots, int shardId);
}
