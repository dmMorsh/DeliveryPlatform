using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Weight : ValueObject
{
    private Weight() { }
    
    public Weight(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}