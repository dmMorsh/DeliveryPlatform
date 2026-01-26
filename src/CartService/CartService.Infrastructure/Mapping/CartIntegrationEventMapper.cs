using CartService.Application.Mapping;
using CartService.Domain.Events;
using Shared.Contracts.Events;

namespace CartService.Infrastructure.Mapping;

public class CartIntegrationEventMapper : ICartIntegrationEventMapper
{
    public CartItemAddedEvent MapCartItemAddedEvent(Guid cartId, Guid productId, int quantity)
    {
        return new CartItemAddedEvent
        {
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity
        };
    }

    public CartCheckedOutEvent MapCartCheckedOutEvent(Guid cartId, Guid customerId)
    {
        return new CartCheckedOutEvent
        {
            CartId = cartId,
            CustomerId = customerId
        };
    }

    public IntegrationEvent? MapFromDomainEvent(Domain.SeedWork.DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CartItemAddedDomainEvent e => new CartItemAddedEvent { CartId = e.CartId, AggregateId = e.CartId, ProductId = e.ProductId, Quantity = e.Quantity, Timestamp = e.OccurredAt },
            CartCheckedOutDomainEvent e => new CartCheckedOutEvent { CartId = e.CartId, AggregateId = e.CartId, CustomerId = e.CustomerId, Timestamp = e.OccurredAt },
            _ => null
        };
    }
}
