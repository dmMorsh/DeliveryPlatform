using CourierService.Domain.Aggregates;
using CourierService.Domain.SeedWork;

namespace CourierService.Domain.Events;

public class CourierRegisteredDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
}

public class CourierStatusChangedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public CourierStatus PreviousStatus { get; init; }
    public CourierStatus NewStatus { get; init; }
}

public class CourierLocationUpdatedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public class CourierRatingUpdatedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public double Rating { get; init; }
}
