using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Events;

public record ProductPriceChanged : DomainEvent
{
    public Guid Id { get; init; }
    public Money OldPrice { get; init; }
    public Money NewPrice { get; init; }
}