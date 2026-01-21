using OrderService.Domain;

namespace OrderService.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct);
    Task<List<Order>> GetOrdersByClientIdAsync(Guid clientId, CancellationToken ct);
    Task<List<Order>> GetOrdersByCourierIdAsync(Guid courierId, CancellationToken ct);
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken ct);
    Task<Order> CreateOrderAsync(Order order, CancellationToken ct);
    void Remove(Order order);
    Task<(List<Order> Items, int Total)> GetOrdersPagedAsync(int page = 1, int pageSize = 20);
}
