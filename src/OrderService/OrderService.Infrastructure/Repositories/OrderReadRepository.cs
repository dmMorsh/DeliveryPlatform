using Microsoft.EntityFrameworkCore;
using OrderService.Application;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly OrderDbContext _db;

    public OrderReadRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = int.Parse(o.Status.ToString()),//o.Status.ToString(),
                FromAddress = o.From.Street,
                ToAddress = o.To.Street,
                CostCents = o.CostCents.Amount,
                // Items = o.Items.Select(i => new OrderItemDto
                // {
                //     ProductId = i.ProductId,
                //     Name = i.Name,
                //     Price = i.Price,
                //     Quantity = i.Quantity
                // }).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OrderView>> GetByClientIdAsync(Guid clientId, CancellationToken ct)
    {
        return await _db.Orders
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o=> new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = int.Parse(o.Status.ToString()),
                FromAddress = o.From.Street,
                ToAddress = o.To.Street,
                CostCents = o.CostCents.Amount,
            })
            .ToListAsync(ct);
    }
}
