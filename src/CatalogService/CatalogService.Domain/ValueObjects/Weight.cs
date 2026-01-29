using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Weight : ValueObject
{
    private Weight() { }
    
    public Weight(long value)
    {
        Value = value;
    }

    public long Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}