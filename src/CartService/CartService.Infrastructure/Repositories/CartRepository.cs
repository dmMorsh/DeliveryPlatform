using CartService.Application.Interfaces;
using CartService.Domain.Aggregates;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CartService.Api.Repositories;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _db;

    public CartRepository(CartDbContext db)
    {
        _db = db;
    }

    public async Task<Cart?> GetCartByCustomerIdAsync(Guid customerId)
    {
        return await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<Cart> CreateOrUpdateAsync(Cart cart)
    {
        var existing = await _db.Carts.FindAsync(cart.Id);
        if (existing == null)
        {
            _db.Carts.Add(cart);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(cart);
            // update owned collection
            _db.Entry(existing).Collection(e => e.Items).CurrentValue = cart.Items.ToList();
        }

        await _db.SaveChangesAsync();
        return cart;
    }
}
