using CourierService.Domain.Events;
using Shared.Contracts.Events;

namespace CourierService.Application.Mapping;

public interface ICourierIntegrationEventMapper
{
    IntegrationEvent? MapFromDomainEvent(CourierDomainEvent domainEvent);
}

public class CourierEventMapper : ICourierIntegrationEventMapper
{
    public IntegrationEvent? MapFromDomainEvent(CourierDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CourierRegisteredDomainEvent e => new CourierRegisteredEvent { CourierId = e.CourierId, Timestamp = e.OccurredAt },
            CourierStatusChangedDomainEvent e => new CourierStatusChangedEvent { CourierId = e.CourierId, PreviousStatus = (int)e.PreviousStatus, NewStatus = (int)e.NewStatus, Timestamp = e.OccurredAt },
            CourierLocationUpdatedDomainEvent e => new CourierLocationUpdatedEvent { CourierId = e.CourierId, Latitude = e.Latitude, Longitude = e.Longitude, Timestamp = e.OccurredAt },
            CourierRatingUpdatedDomainEvent e => new CourierRatingUpdatedEvent { CourierId = e.CourierId, NewRating = e.Rating, TotalRatings = 0, Timestamp = e.OccurredAt },
            _ => null
        };
    }
}
