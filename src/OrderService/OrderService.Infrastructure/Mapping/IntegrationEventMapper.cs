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
            AggregateId = order.Id,
            OrderId = order.Id,
            CourierId = courierId,
            CourierName = courierName,
            CourierPhone = courierPhone
        };
    }

    public OrderStatusChangedEvent MapOrderStatusChangedEvent(Order order, int oldStatus, int newStatus)
    {
        return new OrderStatusChangedEvent
        {
            AggregateId = order.Id,
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow
        };
    }

    public OrderDeliveredEvent MapOrderDeliveredEvent(Order order, Guid courierId)
    {
        return new OrderDeliveredEvent
        {
            AggregateId = order.Id,
            OrderId = order.Id,
            CourierId = courierId,
            DeliveredAt = DateTime.UtcNow
        };
    }

    public IntegrationEvent? MapFromDomainEvent(Domain.SeedWork.DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderCreatedDomainEvent e => new OrderCreatedEvent 
            { 
                AggregateId = e.AggregateId,
                OrderNumber = e.OrderNumber,
                ClientId = e.ClientId,
                FromAddress = e.FromAddress,
                ToAddress = e.ToAddress,
                CostCents = e.CostCents,
                Description = e.Description,
                Timestamp = e.OccurredAt,
                Items = e.Items.Select(i => new OrderEItemSnapshot
                {
                    ProductId = i.ProductId,
                    Name = i.Name,
                    PriceCents = i.PriceCents,
                    Quantity = i.Quantity
                }).ToList()
            },
            OrderAssignedDomainEvent e => new OrderAssignedEvent 
            { 
                AggregateId = e.AggregateId,
                OrderId = e.OrderId, 
                CourierId = e.CourierId, 
                Timestamp = e.OccurredAt 
            },
            OrderStatusChangedDomainEvent e => new OrderStatusChangedEvent 
            { 
                AggregateId = e.AggregateId,
                OrderId = e.OrderId, 
                OldStatus = (int)e.PreviousStatus,
                NewStatus = (int)e.NewStatus,
                Timestamp = e.OccurredAt 
            },
            _ => null
        };
    }
}
