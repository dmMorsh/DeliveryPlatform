using OrderService.Application.Interfaces;
using OrderService.Domain;
using Shared.Contracts.Events;
using System.Linq;

namespace OrderService.Infrastructure.Mapping;

public class IntegrationEventMapper : IOrderIntegrationEventMapper
{
    public OrderCreatedEvent MapToOrderCreatedEvent(Order order)
    {
        return new OrderCreatedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            FromAddress = order.From.Street,
            ToAddress = order.To.Street,
            FromLatitude = order.From.Latitude,
            FromLongitude = order.From.Longitude,
            ToLatitude = order.To.Latitude,
            ToLongitude = order.To.Longitude,
            CostCents = order.CostCents.Amount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemSnapshot
            {
                ProductId = i.ProductId,
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            OrderCreatedDomainEvent e => new OrderCreatedEvent { OrderId = e.OrderId, Timestamp = e.OccurredAt },
            OrderAssignedDomainEvent e => new OrderAssignedEvent { OrderId = e.OrderId, CourierId = e.CourierId, Timestamp = e.OccurredAt },
            OrderStatusChangedDomainEvent e => new OrderStatusChangedEvent { OrderId = e.OrderId, PreviousStatus = (int)e.PreviousStatus, NewStatus = (int)e.NewStatus, Timestamp = e.OccurredAt },
            _ => null
        };
    }
}
