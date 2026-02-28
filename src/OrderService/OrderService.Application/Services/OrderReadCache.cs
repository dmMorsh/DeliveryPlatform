using OrderService.Application.Models;
using Shared.Services;

namespace OrderService.Application.Services;

public static class OrderReadCache
{
    private static readonly MemoryTtlCache<Guid, OrderView?> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    public static bool TryGet(Guid orderId, out OrderView? view) => Cache.TryGet(orderId, out view);

    public static void Set(Guid orderId, OrderView? view) => Cache.Set(orderId, view, CacheTtl);

    public static void Invalidate(Guid orderId) => Cache.Remove(orderId);

    public static async Task<OrderView?> LoadAsync(Func<Task<OrderView?>> loader)
    {
        return await loader().ConfigureAwait(false);
    }
}
