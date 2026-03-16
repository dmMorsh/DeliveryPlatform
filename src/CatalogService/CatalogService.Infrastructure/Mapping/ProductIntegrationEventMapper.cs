using CatalogService.Application.Interfaces;
using CatalogService.Domain.Events;
using CatalogService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CatalogService.Infrastructure.Mapping;

public class ProductIntegrationEventMapper : IProductIntegrationEventMapper
{
    public ProductPriceChangedEvent MapProductPriceChangedEvent(ProductPriceChanged e)
    {
        return new ProductPriceChangedEvent
        {
            ProductId = e.Id,
            OldPriceCents = e.OldPrice.AmountCents,
            NewPriceCents = e.NewPrice.AmountCents
        };
    }

    public ProductCreatedEvent MapProductCreatedEvent(ProductCreated e)
    {
        return new ProductCreatedEvent
        {
            ProductId = e.Id,
            Name = e.Name,
            Description = e.Description,
            PriceCents = e.PriceCents.AmountCents,
            Currency = e.PriceCents.Currency,
            WeightGrams = e.WeightGrams.Value,
            QuantityAvailable = e.QuantityAvailable
        };
    }

    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            ProductCreated e => MapProductCreatedEvent(e),
            ProductPriceChanged e => MapProductPriceChangedEvent(e),
            _ => null
        };
    }
}
