using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IReservationRepository
{
    Task AddReservationAsync(StockReservation item, CancellationToken ct);

    Task<bool> ReservationExistAsync(Guid orderId, CancellationToken ct);
    
    Task<List<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct);
}