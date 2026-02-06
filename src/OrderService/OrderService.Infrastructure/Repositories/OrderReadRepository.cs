using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly OrderDbContext _context;

    public OrderReadRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                FromAddress = o.From.Street,
                ToAddress = o.To.Street,
                CostCents = o.CostCents.AmountCents,
                Items = o.Items.Select(i => new OrderViewItem(i.ProductId,i.Name,i.PriceCents, i.Quantity)).ToArray()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OrderView>> GetByClientIdAsync(Guid clientId, CancellationToken ct)
    {
        return await _context.Orders
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o=> new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                FromAddress = o.From.Street,
                ToAddress = o.To.Street,
                CostCents = o.CostCents.AmountCents,
                Items = o.Items.Select(i => new OrderViewItem(i.ProductId,i.Name,i.PriceCents, i.Quantity)).ToArray()
            })
            .ToListAsync(ct);
    }
}
