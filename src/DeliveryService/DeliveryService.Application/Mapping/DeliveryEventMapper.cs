using DeliveryService.Domain.Events;
using Shared.Contracts.Events;
using DomainEvent = DeliveryService.Domain.SeedWork.DomainEvent;

namespace DeliveryService.Application.Mapping;

public interface IDeliveryEventMapper
{
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}

public class DeliveryEventMapper : IDeliveryEventMapper
{
    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            DeliveryCreatedDomainEvent e => new DeliveryCreatedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                Timestamp = e.OccurredAt
            },
            DeliveryAssignedDomainEvent e => new DeliveryAssignedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Timestamp = e.OccurredAt
            },
            DeliveryAcceptedDomainEvent e => new DeliveryAcceptedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Timestamp = e.OccurredAt
            },
            DeliveryDeclinedDomainEvent e => new DeliveryDeclinedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Reason = e.Reason,
                Timestamp = e.OccurredAt
            },
            DeliveryPickedUpDomainEvent e => new DeliveryPickedUpEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Timestamp = e.OccurredAt
            },
            DeliveryInTransitDomainEvent e => new DeliveryInTransitEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Timestamp = e.OccurredAt
            },
            DeliveryDeliveredDomainEvent e => new DeliveryDeliveredEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Signature = e.Signature,
                PhotoUrl = e.PhotoUrl,
                Notes = e.Notes,
                Timestamp = e.OccurredAt
            },
            DeliveryCancelledDomainEvent e => new DeliveryCancelledEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Reason = e.Reason,
                Timestamp = e.OccurredAt
            },
            DeliveryFailedDomainEvent e => new DeliveryFailedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Reason = e.Reason,
                Timestamp = e.OccurredAt
            },
            DeliveryReturnedDomainEvent e => new DeliveryReturnedEvent
            {
                DeliveryId = e.DeliveryId,
                OrderId = e.OrderId,
                CourierId = e.CourierId,
                Reason = e.Reason,
                Timestamp = e.OccurredAt
            },
            _ => null
        };
    }
}
