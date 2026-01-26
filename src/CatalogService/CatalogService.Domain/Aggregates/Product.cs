using CatalogService.Domain.Events;
using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Aggregates;

public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public Weight Weight { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    private Product() {}
    
    public Product(string name, string? description, Money price, Weight weight)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Price = price;
        Weight = weight;
        IsActive = true;
    }

    public void ChangePrice(Money newPrice)
    {
        if (newPrice.Equals(Price)) return;

        var oldPrice = Price;
        Price = newPrice;
        AddDomainEvent(new ProductPriceChanged(Id, oldPrice, newPrice));
    }

    public void ChangeWeight(Weight newWeight)
    {
        if (newWeight.Equals(Weight)) return;
        // var oldWeight = Weight;
        Weight = newWeight;
    }

    public void ChangeDescription(string newDescription)
    {
        if (newDescription.Equals(Description)) return;
        // var oldDescription = Description;
        Description = newDescription;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
