using CatalogService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CatalogService.Application.Interfaces;

public interface IProductIntegrationEventMapper
{
    IntegrationEvent? MapFromDomainEvent (DomainEvent arg);
}
