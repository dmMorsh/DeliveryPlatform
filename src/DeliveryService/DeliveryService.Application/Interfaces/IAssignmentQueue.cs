namespace DeliveryService.Application.Interfaces;

public interface IAssignmentQueue
{
    Task EnqueueAsync(Guid deliveryId, DateTimeOffset availableAt, bool onlyIfMissing = false, CancellationToken ct = default);
    Task<Guid?> DequeueReadyAsync(DateTimeOffset now, CancellationToken ct = default);
}
