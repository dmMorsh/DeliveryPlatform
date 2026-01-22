using CartService.Application.Interfaces;
using CartService.Domain.Aggregates;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Api.Repositories;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
    }

    public async Task<Cart> CreateOrUpdateAsync(Cart cart, CancellationToken ct = default)
    {
        var existing = await _context.Carts.FindAsync(cart.Id);
        if (existing == null)
        {
            _context.Carts.Add(cart);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(cart);
            // update owned collection
            _context.Entry(existing).Collection(e => e.Items).CurrentValue = cart.Items.ToList();
        }

        await _context.SaveChangesAsync();
        return cart;
    }
}
