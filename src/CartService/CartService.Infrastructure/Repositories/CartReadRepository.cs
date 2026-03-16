using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Repositories;

public class CartReadRepository : ICartReadRepository
{
    private readonly CartDbContext _context;
    private readonly ICartReadCache _cache;

    public CartReadRepository(CartDbContext context, ICartReadCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<CartView?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        // Try cache
        var cached = await _cache.GetAsync(customerId, ct);
        if (cached != null) return cached;

        // Fallback to DB
        var cart = await _context.Carts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .Select(c => new CartView
            { 
                Id = c.Id,
                Items = c.Items.Select(ci => new CartViewItem(ci.ProductId, ci.Name, ci.PriceCents, ci.Quantity)).ToArray(), 
            })
            .FirstOrDefaultAsync(ct);

        // Cache result
        if (cart != null)
        {
            await _cache.SetAsync(customerId, cart, ct);
        }
        return cart;
    }
}
