using CartService.Application.Interfaces;
using CartService.Domain.Aggregates;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct)
    {
        return await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
    }

    public async Task AddAsync(Cart cart, CancellationToken ct)
    {
        await _context.Carts.AddAsync(cart, ct);
    }
}
