using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace CartService.Infrastructure.Repositories;

public class CartReadRepository : ICartReadRepository
{
    private readonly CartDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public CartReadRepository(CartDbContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis;
    }

    public async Task<CartView?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        // Try cache
        var cached = await GetFromCacheAsync(customerId, ct);
        if (cached != null) return cached;

        // Fallback to DB
        var cart = await _context.Carts
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
            await SetToCacheAsync(customerId, cart, ct);
        }
        return cart;
    }

    private async Task<CartView?> GetFromCacheAsync(Guid customerId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CacheKey(customerId));
        if (!value.HasValue) return null;
        return JsonSerializer.Deserialize<CartView>(value.ToString());
    }

    private async Task SetToCacheAsync(Guid customerId, CartView cart, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(cart);
        await db.StringSetAsync(CacheKey(customerId), json, CacheTtl);
    }

    public async Task InvalidateCacheAsync(Guid customerId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKey(customerId));
    }

    private static RedisKey CacheKey(Guid customerId) => $"cart:{customerId}";
}