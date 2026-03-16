using CartService.Application.Models;
using CartService.Domain.Aggregates;

namespace CartService.Application.Mapping;

internal static class CartViewMapper
{
    public static CartView ToView(Cart cart)
    {
        return new CartView
        {
            Id = cart.Id,
            Items = cart.Items
                .Select(i => new CartViewItem(i.ProductId, i.Name, i.PriceCents, i.Quantity))
                .ToArray()
        };
    }
}
