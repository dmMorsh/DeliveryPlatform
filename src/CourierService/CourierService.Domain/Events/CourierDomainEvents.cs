using CourierService.Domain.Aggregates;
using CourierService.Domain.SeedWork;

namespace CourierService.Domain.Events;

public record CourierRegisteredDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
}

public record CourierStatusChangedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public CourierStatus PreviousStatus { get; init; }
    public CourierStatus NewStatus { get; init; }
}

public record CourierLocationUpdatedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public record CourierRatingUpdatedDomainEvent : DomainEvent
{
    public Guid CourierId { get; init; }
    public double Rating { get; init; }
}
