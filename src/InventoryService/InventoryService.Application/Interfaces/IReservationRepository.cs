using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IReservationRepository
{
    Task AddReservationAsync(StockReservation item, CancellationToken ct);

    Task<bool> ReservationExistAsync(Guid orderId, Guid productId, CancellationToken ct);

    Task<List<Guid>> GetReservedProductIdsAsync(Guid orderId, IEnumerable<Guid> productIds, CancellationToken ct);
    
    Task<List<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct);

    Task<List<Guid>> GetStaleOrderIdsAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct);
}
