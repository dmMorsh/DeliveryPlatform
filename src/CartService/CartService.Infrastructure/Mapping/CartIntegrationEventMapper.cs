using CartService.Application.Interfaces;
using CartService.Domain.Events;
using CartService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CartService.Infrastructure.Mapping;

public class CartIntegrationEventMapper : ICartIntegrationEventMapper
{
    private CartItemAddedEvent MapCartItemAddedEvent(CartItemAddedDomainEvent e)
    {
        return new CartItemAddedEvent
        {
            CartId = e.CartId,
            ProductId = e.ProductId,
            Quantity = e.Quantity,
            Timestamp = e.OccurredAt
        };
    }

    private CartCheckedOutEvent MapCartCheckedOutEvent(CartCheckedOutDomainEvent e)
    {
        return new CartCheckedOutEvent
        {
            CartId = e.CartId,
            CustomerId = e.CustomerId,
            Timestamp = e.OccurredAt
        };
    }

    public IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CartItemAddedDomainEvent e => MapCartItemAddedEvent(e),
            CartCheckedOutDomainEvent e => MapCartCheckedOutEvent(e),
            _ => null
        };
    }
}
