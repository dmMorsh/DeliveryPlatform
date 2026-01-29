using CatalogService.Domain.Events;
using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Aggregates;

public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money PriceCents { get; private set; }
    public Weight WeightGrams { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    private Product() {}
    
    public Product(string name, string? description, Money priceCents, Weight weightGrams)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        PriceCents = priceCents;
        WeightGrams = weightGrams;
        IsActive = true;
    }

    public void ChangePrice(Money newPrice)
    {
        if (newPrice.Equals(PriceCents)) return;

        var oldPrice = PriceCents;
        PriceCents = newPrice;
        AddDomainEvent(new ProductPriceChanged{ Id = Id, OldPrice = oldPrice, NewPrice = newPrice});
    }

    public void ChangeWeight(Weight newWeight)
    {
        if (newWeight.Equals(WeightGrams)) return;
        // var oldWeight = Weight;
        WeightGrams = newWeight;
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
