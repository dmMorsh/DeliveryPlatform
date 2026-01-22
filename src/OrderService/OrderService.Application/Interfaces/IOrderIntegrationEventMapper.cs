using OrderService.Domain;
using OrderService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace OrderService.Application.Interfaces;

public interface IOrderIntegrationEventMapper
{
    OrderCreatedEvent MapToOrderCreatedEvent(Order order);
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}
