namespace OrderService.Application.Interfaces;

public interface IKitchenSlotReadRepository
{
    Task<int> CountSlotAsync(DateTime slotStart, CancellationToken ct);
    Task<DateTime?> FindNextAvailableSlotAsync(DateTime slotStart, int slotMinutes, int capacity, int lookaheadSlots, CancellationToken ct);
}
