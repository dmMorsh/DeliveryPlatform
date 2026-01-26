using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Events;

public class ProductPriceChanged : DomainEvent
{
    public ProductPriceChanged(Guid id, Money oldPrice, Money newPrice)
    {
        Id = id;
        OldPrice = oldPrice;
        NewPrice = newPrice;
    }

    public Guid Id { get; set; }
    public Money OldPrice { get; set; }
    public Money NewPrice { get; set; }
}