using Shared.Contracts.Events;

namespace CatalogService.Application.Interfaces;

public interface IProductIntegrationEventMapper
{
    ProductPriceChangedEvent MapProductPriceChangedEvent(Guid productId, decimal oldPriceCents, decimal newPriceCents);
    ProductCreatedEvent MapProductCreatedEvent(Guid productId, string name, string description, int priceCents, int quantityAvailable);
}
