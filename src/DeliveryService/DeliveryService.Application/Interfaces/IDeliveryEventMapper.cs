using DeliveryService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace DeliveryService.Application.Interfaces;

public interface IDeliveryEventMapper
{
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}