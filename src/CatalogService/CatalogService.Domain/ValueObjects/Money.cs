using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Money : ValueObject
{
    private Money() { }
    
    public Money(long amountCents, string currency)
    {
        AmountCents = amountCents;
        Currency = currency;
    }

    public long AmountCents { get; private set; }
    public string Currency { get; private set; }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AmountCents;
        yield return Currency;
    }
}