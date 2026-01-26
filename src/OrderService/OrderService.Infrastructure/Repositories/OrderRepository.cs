using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Domain.Aggregates;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Orders.FindAsync(id, ct);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct)
    {
        return await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<List<Order>> GetOrdersByClientIdAsync(Guid clientId, CancellationToken ct)
    {
        return await _context.Orders
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Order>> GetOrdersByCourierIdAsync(Guid courierId, CancellationToken ct)
    {
        return await _context.Orders
            .Where(o => o.CourierId == courierId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken ct)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Order> CreateOrderAsync(Order order, CancellationToken ct)
    {
        await _context.Orders.AddAsync(order, ct);
        return order;
    }

    public void Remove(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task<(List<Order> Items, int Total)> GetOrdersPagedAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.Orders.OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
