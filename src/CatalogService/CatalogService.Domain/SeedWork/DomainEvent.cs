namespace CatalogService.Domain.SeedWork;

public abstract class DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
