using CourierService.Domain.Events;
using Shared.Contracts.Events;
using DomainEvent = CourierService.Domain.SeedWork.DomainEvent;

namespace CourierService.Application.Mapping;

public interface ICourierEventMapper
{
    CourierStatusChangedEvent MapCourierStatusChangedEvent(Guid courierId, int oldStatus, int newStatus);
    CourierLocationUpdatedEvent MapLocationUpdatedEvent(Guid courierId, double latitude, double longitude);
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}

public class CourierEventMapper : ICourierEventMapper
{
    public CourierStatusChangedEvent MapCourierStatusChangedEvent(Guid courierId, int oldStatus, int newStatus)
    {
        return new CourierStatusChangedEvent
        {
            AggregateId = courierId,
            CourierId = courierId,
            PreviousStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow
        };
    }

    public CourierLocationUpdatedEvent MapLocationUpdatedEvent(Guid courierId, double latitude, double longitude)
    {
        return new CourierLocationUpdatedEvent
        {
            AggregateId = courierId,
            CourierId = courierId,
            Latitude = latitude,
            Longitude = longitude,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CourierStatusChangedDomainEvent e => new CourierStatusChangedEvent 
            { 
                AggregateId = e.CourierId,
                CourierId = e.CourierId, 
                PreviousStatus = (int)e.PreviousStatus,
                NewStatus = (int)e.NewStatus,
                Timestamp = e.OccurredAt 
            },
            CourierLocationUpdatedDomainEvent e => new CourierLocationUpdatedEvent 
            { 
                AggregateId = e.CourierId,
                CourierId = e.CourierId, 
                Latitude = e.Latitude, 
                Longitude = e.Longitude, 
                Timestamp = e.OccurredAt 
            },
            
            CourierRegisteredDomainEvent e => new CourierRegisteredEvent
            {
                AggregateId = e.CourierId,
                CourierId = e.CourierId, 
                Timestamp = e.OccurredAt
            },
            CourierRatingUpdatedDomainEvent e => new CourierRatingUpdatedEvent
            {
                AggregateId = e.CourierId,
                CourierId = e.CourierId, 
                NewRating = e.Rating, 
                TotalRatings = 0, 
                Timestamp = e.OccurredAt
            },

            _ => null
        };
    }
}
