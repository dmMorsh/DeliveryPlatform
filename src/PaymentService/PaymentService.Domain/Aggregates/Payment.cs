namespace PaymentService.Domain.Aggregates;

public class Payment
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Payment() { }

    public static Payment Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new Payment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }
}
