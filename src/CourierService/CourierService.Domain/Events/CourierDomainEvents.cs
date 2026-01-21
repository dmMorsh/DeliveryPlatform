using CourierService.Domain.Aggregates;

namespace CourierService.Domain.Events;

public abstract class CourierDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class CourierRegisteredDomainEvent : CourierDomainEvent
{
    public Guid CourierId { get; init; }
}

public class CourierStatusChangedDomainEvent : CourierDomainEvent
{
    public Guid CourierId { get; init; }
    public CourierStatus PreviousStatus { get; init; }
    public CourierStatus NewStatus { get; init; }
}

public class CourierLocationUpdatedDomainEvent : CourierDomainEvent
{
    public Guid CourierId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public class CourierRatingUpdatedDomainEvent : CourierDomainEvent
{
    public Guid CourierId { get; init; }
    public double Rating { get; init; }
}
