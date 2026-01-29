using CartService.Domain.Events;
using CartService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CartService.Application.Mapping;

public interface ICartIntegrationEventMapper
{
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}

public class CartEventMapper : ICartIntegrationEventMapper
{
    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CartItemAddedDomainEvent e => new CartItemAddedEvent
            {
                CartId = e.CartId, 
                ProductId = e.ProductId,
                Quantity = e.Quantity,
                Timestamp = e.OccurredAt
            },
            CartCheckedOutDomainEvent e => new CartCheckedOutEvent
            {
                CartId = e.CartId,
                CustomerId = e.CustomerId,
                Timestamp = e.OccurredAt
            },
            _ => null
        };
    }
}
