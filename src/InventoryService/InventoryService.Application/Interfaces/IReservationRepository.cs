using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IReservationRepository
{
    Task AddReservationAsync(StockReservation item, CancellationToken ct);

    Task<bool> ReservationExistAsync(Guid orderId, CancellationToken ct);
    
    Task<IEnumerable<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct);
}