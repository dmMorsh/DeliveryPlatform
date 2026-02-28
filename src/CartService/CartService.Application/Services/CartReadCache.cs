using CartService.Application.Models;
using Shared.Services;

namespace CartService.Application.Services;

internal static class CartReadCache
{
    private static readonly MemoryTtlCache<Guid, CartView?> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    public static bool TryGet(Guid customerId, out CartView? view) => Cache.TryGet(customerId, out view);

    public static void Set(Guid customerId, CartView? view) => Cache.Set(customerId, view, CacheTtl);

    public static void Invalidate(Guid customerId) => Cache.Remove(customerId);

    public static async Task<CartView?> LoadAsync(Func<Task<CartView?>> loader)
    {
        return await loader().ConfigureAwait(false);
    }
}
