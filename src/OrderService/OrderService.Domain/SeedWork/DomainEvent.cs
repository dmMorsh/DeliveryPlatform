namespace OrderService.Domain.SeedWork;

public abstract class DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}