using CatalogReadService.Application.Models;
using Shared.Services;

namespace CatalogReadService.Application.Services;

internal static class ProductReadCache
{
    private static readonly MemoryTtlCache<Guid, ProductView?> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    public static bool TryGet(Guid productId, out ProductView? view) => Cache.TryGet(productId, out view);

    public static void Set(Guid productId, ProductView? view) => Cache.Set(productId, view, CacheTtl);

    public static void Invalidate(Guid productId) => Cache.Remove(productId);

    public static async Task<ProductView?> LoadAsync(Func<Task<ProductView?>> loader)
    {
        return await loader().ConfigureAwait(false);
    }
}
