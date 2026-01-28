using Shared.Contracts.Events;

namespace CatalogService.Application.Interfaces;

public interface IProductIntegrationEventMapper
{
    ProductPriceChangedEvent MapProductPriceChangedEvent(Guid productId, long oldPriceCents, long newPriceCents);
    ProductCreatedEvent MapProductCreatedEvent(Guid productId, string name, string description, long priceCents, int quantityAvailable);
}
