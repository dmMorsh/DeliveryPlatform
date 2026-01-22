using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Weight : ValueObject
{
    private Weight() { }
    
    public Weight(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}