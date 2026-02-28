using CourierService.Domain.Aggregates;
using Shared.Services;

namespace CourierService.Application.Services;

internal static class CourierReadCache
{
    private static readonly MemoryTtlCache<Guid, Courier?> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    public static bool TryGet(Guid courierId, out Courier? courier) => Cache.TryGet(courierId, out courier);

    public static void Set(Guid courierId, Courier? courier) => Cache.Set(courierId, courier, CacheTtl);

    public static void Invalidate(Guid courierId) => Cache.Remove(courierId);

    public static async Task<Courier?> LoadAsync(Func<Task<Courier?>> loader)
    {
        return await loader().ConfigureAwait(false);
    }
}
