using CatalogService.Domain.SeedWork;

namespace CatalogService.Domain.ValueObjects;

public class Money : ValueObject
{
    private Money() { }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}