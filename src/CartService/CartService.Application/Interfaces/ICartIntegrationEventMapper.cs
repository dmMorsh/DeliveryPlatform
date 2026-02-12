using CartService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CartService.Application.Interfaces;

public interface ICartIntegrationEventMapper
{
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}