using DeliveryService.Domain.Aggregates;
using Shared.Services;

namespace DeliveryService.Application.Services;

internal static class DeliveryReadCache
{
    private static readonly MemoryTtlCache<Guid, Delivery?> ByDeliveryId = new();
    private static readonly MemoryTtlCache<Guid, Delivery?> ByOrderId = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    public static bool TryGetByDeliveryId(Guid deliveryId, out Delivery? delivery)
        => ByDeliveryId.TryGet(deliveryId, out delivery);

    public static void SetByDeliveryId(Guid deliveryId, Delivery? delivery)
        => ByDeliveryId.Set(deliveryId, delivery, CacheTtl);

    public static bool TryGetByOrderId(Guid orderId, out Delivery? delivery)
        => ByOrderId.TryGet(orderId, out delivery);

    public static void SetByOrderId(Guid orderId, Delivery? delivery)
        => ByOrderId.Set(orderId, delivery, CacheTtl);

    public static void Invalidate(Guid deliveryId, Guid orderId)
    {
        ByDeliveryId.Remove(deliveryId);
        ByOrderId.Remove(orderId);
    }

    public static void InvalidateByDeliveryId(Guid deliveryId) => ByDeliveryId.Remove(deliveryId);

    public static void InvalidateByOrderId(Guid orderId) => ByOrderId.Remove(orderId);

    public static async Task<Delivery?> LoadAsync(Func<Task<Delivery?>> loader)
    {
        return await loader().ConfigureAwait(false);
    }
}
