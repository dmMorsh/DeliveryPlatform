using OrderService.Domain.Aggregates;
using Shared.Contracts.Events;

namespace OrderService.Application.Interfaces;

public interface IOrderIntegrationEventMapper
{
    OrderAssignedEvent MapOrderAssignedEvent(Order order, Guid courierId, string courierName, string? courierPhone = null);
    OrderStatusChangedEvent MapOrderStatusChangedEvent(Order order, int oldStatus, int newStatus);
    OrderDeliveredEvent MapOrderDeliveredEvent(Order order, Guid courierId);
    IntegrationEvent? MapFromDomainEvent(Domain.SeedWork.DomainEvent domainEvent);
}
