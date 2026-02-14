using CourierService.Application.Interfaces;
using CourierService.Domain.Events;
using Shared.Contracts.Events;
using DomainEvent = CourierService.Domain.SeedWork.DomainEvent;

namespace CourierService.Infrastructure.Mapping;

public class CourierEventMapper : ICourierEventMapper
{
    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CourierStatusChangedDomainEvent e => new CourierStatusChangedEvent 
            { 
                CourierId = e.CourierId, 
                PreviousStatus = (int)e.PreviousStatus,
                NewStatus = (int)e.NewStatus,
                ChangedAt = DateTime.UtcNow,
                Timestamp = e.OccurredAt 
            },
            CourierLocationUpdatedDomainEvent e => new CourierLocationUpdatedEvent 
            { 
                CourierId = e.CourierId, 
                Latitude = e.Latitude, 
                Longitude = e.Longitude, 
                UpdatedAt = DateTime.UtcNow,
                Timestamp = e.OccurredAt 
            },
            
            CourierRegisteredDomainEvent e => new CourierRegisteredEvent
            {
                CourierId = e.CourierId, 
                Timestamp = e.OccurredAt
            },
            CourierRatingUpdatedDomainEvent e => new CourierRatingUpdatedEvent
            {
                CourierId = e.CourierId, 
                NewRating = e.Rating, 
                TotalRatings = 0, 
                Timestamp = e.OccurredAt
            },

            _ => null
        };
    }
}
