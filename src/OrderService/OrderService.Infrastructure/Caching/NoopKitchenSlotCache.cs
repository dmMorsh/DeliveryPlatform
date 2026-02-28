using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Caching;

public sealed class NoopKitchenSlotCache : IKitchenSlotCache
{
    public Task<int> GetCountAsync(DateTime slotStart, CancellationToken ct)
    {
        return Task.FromResult(0);
    }

    public Task<bool> TryReserveAsync(DateTime slotStart, int capacity, TimeSpan ttl, CancellationToken ct)
    {
        // No-op cache allows reservation (acts as disabled)
        return Task.FromResult(true);
    }

    public Task ReleaseAsync(DateTime slotStart, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
