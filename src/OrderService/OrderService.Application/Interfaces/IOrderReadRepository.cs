using OrderService.Application.Models;

namespace OrderService.Application.Interfaces;

public interface IOrderReadRepository
{
    Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct);
    
    Task<List<OrderView>> GetByClientIdAsync(Guid clientId, CancellationToken ct);

}