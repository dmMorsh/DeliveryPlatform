using CatalogService.Domain.Events;
using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Aggregates;

public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public Weight Weight { get; private set; }
    public bool IsActive { get; private set; }

    public Product(string name, Money price, Weight weight)
    {
        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        Weight = weight;
        IsActive = true;
    }

    public void ChangePrice(Money newPrice)
    {
        if (newPrice.Equals(Price)) return;

        Price = newPrice;
        AddDomainEvent(new ProductPriceChanged(Id, newPrice));
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
