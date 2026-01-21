using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Weight(decimal value) : ValueObject
{
    public decimal Value { get; } = value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}