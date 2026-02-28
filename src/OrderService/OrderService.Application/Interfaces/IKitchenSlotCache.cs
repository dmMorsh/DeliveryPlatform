namespace OrderService.Application.Interfaces;

public interface IKitchenSlotCache
{
    Task<int> GetCountAsync(DateTime slotStart, CancellationToken ct);
    Task<bool> TryReserveAsync(DateTime slotStart, int capacity, TimeSpan ttl, CancellationToken ct);
    Task ReleaseAsync(DateTime slotStart, CancellationToken ct);
}
