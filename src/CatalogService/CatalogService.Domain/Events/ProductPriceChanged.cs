using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Events;

public record ProductPriceChanged : DomainEvent
{
    public Guid Id { get; init; }
    public Money OldPrice { get; init; }
    public Money NewPrice { get; init; }
}

public record ProductCreated : DomainEvent
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money PriceCents { get; init; }
    public int QuantityAvailable { get; init; }
}