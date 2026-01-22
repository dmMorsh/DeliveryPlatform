using CartService.Domain.Events;
using Shared.Contracts.Events;

namespace CartService.Application.Mapping;

public interface ICartIntegrationEventMapper
{
    IntegrationEvent? MapFromDomainEvent(CartDomainEvent domainEvent);
}

public class CartEventMapper : ICartIntegrationEventMapper
{
    public IntegrationEvent? MapFromDomainEvent(CartDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            CartItemAddedDomainEvent e => new CartItemAddedEvent { CartId = e.CartId, ProductId = e.ProductId, Quantity = e.Quantity, Timestamp = e.OccurredAt },
            CartCheckedOutDomainEvent e => new CartCheckedOutEvent { CartId = e.CartId, CustomerId = e.CustomerId, Timestamp = e.OccurredAt },
            _ => null
        };
    }
}
