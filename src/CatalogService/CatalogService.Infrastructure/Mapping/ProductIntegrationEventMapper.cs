using CatalogService.Application.Interfaces;
using Shared.Contracts.Events;

namespace CatalogService.Infrastructure.Mapping;

public class ProductIntegrationEventMapper : IProductIntegrationEventMapper
{
    public ProductPriceChangedEvent MapProductPriceChangedEvent(Guid productId, long oldPriceCents,
        long newPriceCents)
    {
        return new ProductPriceChangedEvent
        {
            ProductId = productId,
            AggregateId = productId,
            OldPriceCents = oldPriceCents,
            NewPriceCents = newPriceCents
        };
    }

    public ProductCreatedEvent MapProductCreatedEvent(Guid productId, string name, string description, long priceCents, int quantityAvailable)
    {
        return new ProductCreatedEvent
        {
            ProductId = productId,
            AggregateId = productId,
            Name = name,
            Description = description,
            PriceCents = priceCents,
            QuantityAvailable = quantityAvailable
        };
    }
}
