using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Repositories;

public class CartReadRepository : ICartReadRepository
{
    private readonly CartDbContext _context;

    public CartReadRepository(CartDbContext context)
    {
        _context = context;
    }

    public async Task<CartView?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.Carts
            .Where(c => c.CustomerId == customerId)
            .Select(c => new CartView
            { 
                Id = c.Id,
                Items = c.Items.Select(ci => new CartViewItem(ci.ProductId, ci.Name, ci.PriceCents, ci.Quantity)).ToArray(), 
            })
            .FirstOrDefaultAsync(ct);
    }
}