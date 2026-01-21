using OrderService.Domain;
using Shared.Contracts.Events;

namespace OrderService.Application.Interfaces;

public interface IOrderIntegrationEventMapper
{
    OrderCreatedEvent MapToOrderCreatedEvent(Order order);
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}
