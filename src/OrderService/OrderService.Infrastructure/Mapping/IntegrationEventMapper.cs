using OrderService.Application.Interfaces;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Events;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.Mapping;

public class IntegrationEventMapper : IOrderIntegrationEventMapper
{
    public OrderAssignedEvent MapOrderAssignedEvent(Order order, Guid courierId, string courierName, string? courierPhone = null)
    {
        return new OrderAssignedEvent
        {
            OrderId = order.Id,
            CourierId = courierId,
            CourierName = courierName,
            CourierPhone = courierPhone
        };
    }

    public OrderStatusChangedEvent MapOrderStatusChangedEvent(Order order, OrderStatus oldStatus, OrderStatus newStatus)
    {
        return new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OldStatus = (int)oldStatus,
            PreviousStatus = (int)oldStatus,
            NewStatus = (int)newStatus,
            ChangedAt = DateTime.UtcNow
        };
    }

    public OrderDeliveredEvent MapOrderDeliveredEvent(Order order, Guid courierId)
    {
        return new OrderDeliveredEvent
        {
            OrderId = order.Id,
            CourierId = courierId,
            DeliveredAt = DateTime.UtcNow
        };
    }

    public IntegrationEvent? MapFromDomainEvent(Domain.SeedWork.DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderCreatedDomainEvent e => MapFromOrderCreatedDomainEvent(e, null)
            ,
            OrderItemsReleaseDomainEvent e => MapFromOrderItemsReleaseDomainEvent(e)
            ,
            OrderAssignedDomainEvent e => new OrderAssignedEvent 
            { 
                OrderId = e.OrderId, 
                CourierId = e.CourierId, 
                CourierName = e.CourierName ?? string.Empty,
                CourierPhone = e.CourierPhone,
                Timestamp = e.OccurredAt 
            },
            OrderStatusChangedDomainEvent e => new OrderStatusChangedEvent 
            { 
                OrderId = e.OrderId, 
                OldStatus = (int)e.PreviousStatus,
                PreviousStatus = (int)e.PreviousStatus,
                NewStatus = (int)e.NewStatus,
                Timestamp = e.OccurredAt 
            },
            OrderReadyDomainEvent e => new OrderReadyEvent
            {
                OrderId = e.OrderId,
                ReadyAt = e.ReadyAt,
                Timestamp = e.OccurredAt
            },
            OrderAcceptedDomainEvent e => new OrderAcceptedEvent
            {
                OrderId = e.OrderId,
                AcceptedAt = e.AcceptedAt,
                Timestamp = e.OccurredAt
            },
            OrderRejectedDomainEvent e => new OrderRejectedEvent
            {
                OrderId = e.OrderId,
                RejectedAt = e.RejectedAt,
                Reason = e.Reason,
                Timestamp = e.OccurredAt
            },
            OrderCanceledDomainEvent e => new OrderCanceledEvent
            {
                OrderId = e.OrderId,
                CourierId = e.CourierId ?? Guid.Empty,
                Timestamp = e.OccurredAt
            },
            OrderCriticalErrorDomainEvent e => MapFromOrderCriticalErrorDomainEvent(e)
            ,
            _ => null
        };
    }

    public IntegrationEvent MapFromOrderCreatedDomainEvent(OrderCreatedDomainEvent e, IEnumerable<DomainOrderItemSnapshot>? snapshots)
    {
        return new OrderCreatedEvent
        {
            OrderId = e.OrderId,
            OrderNumber = e.OrderNumber,
            ClientId = e.ClientId,
            FromAddress = e.FromAddress,
            ToAddress = e.ToAddress,
            FromLatitude = e.FromLatitude,
            FromLongitude = e.FromLongitude,
            ToLatitude = e.ToLatitude,
            ToLongitude = e.ToLongitude,
            WeightGrams = e.WeightGrams,
            CostCents = e.CostCents,
            Currency = e.Currency,
            CourierNote = e.CourierNote,
            Description = e.Description,
            CreatedAt = e.CreatedAt,
            ExpectedReadyAt = e.ExpectedReadyAt,
            KitchenSlotStart = e.KitchenSlotStart,
            DeliveryZoneId = e.DeliveryZoneId,
            DeliveryZoneName = e.DeliveryZoneName,
            DeliveryZoneDistanceKm = e.DeliveryZoneDistanceKm,
            DeliveryPickupSlaMinutes = e.DeliveryPickupSlaMinutes,
            DeliveryTransitSlaMinutes = e.DeliveryTransitSlaMinutes,
            DeliveryFeeMultiplier = e.DeliveryFeeMultiplier,
            Timestamp = e.OccurredAt,
            Items = (snapshots ?? e.Items).Select(i => new IntegrationOrderItemSnapshot
            {
                ProductId = i.ProductId,
                Name = i.Name,
                PriceCents = i.PriceCents,
                Quantity = i.Quantity
            }).ToList()
        };
    }
    
    public IntegrationEvent MapFromOrderItemsReleaseDomainEvent(OrderItemsReleaseDomainEvent e)
    {
        return new StockReservationReleaseRequestedEvent
        {
            OrderId = e.OrderId,
            Items = e.Items.Select(i => new IntegrationOrderItemSnapshot
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };
    }
    
    public IntegrationEvent MapFromOrderCriticalErrorDomainEvent(OrderCriticalErrorDomainEvent e)
    {
        return new OrderCriticalErrorEvent
        {
            OrderId = e.OrderId,
            ClientId = e.ClientId,
            Description = e.Description,
        };
    }
}
