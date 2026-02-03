namespace CourierService.Domain.SeedWork;

public abstract class AggregateRoot : Entity
{
    public byte[] RowVersion { get; private set; }
    
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(DomainEvent evt)
        => _domainEvents.Add(evt);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
