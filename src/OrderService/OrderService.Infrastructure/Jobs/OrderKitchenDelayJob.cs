using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.Jobs;

public sealed class OrderKitchenDelayJob : IOrderKitchenDelayJob
{
    private readonly OrderDbContext _db;
    private readonly ILogger<OrderKitchenDelayJob> _logger;
    private readonly int _batchSize;
    private readonly int _maxBatches;

    public OrderKitchenDelayJob(
        OrderDbContext db,
        IConfiguration config,
        ILogger<OrderKitchenDelayJob> logger)
    {
        _db = db;
        _logger = logger;
        _batchSize = int.TryParse(config["Order:KitchenDelayBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Order:KitchenDelayMaxBatches"], out var maxValue) ? maxValue : 20;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var batches = 0;

        while (!ct.IsCancellationRequested && batches < _maxBatches)
        {
            var overdueIds = await _db.Orders.AsNoTracking()
                .Where(o => o.KitchenDelayedNotifiedAt == null
                            && o.ExpectedReadyAt != null
                            && o.ExpectedReadyAt < now
                            && o.IsReadyForDelivery == false
                            && o.Status != OrderStatus.Cancelled
                            && o.Status != OrderStatus.Failed
                            && o.Status != OrderStatus.Delivered)
                .OrderBy(o => o.ExpectedReadyAt)
                .Select(o => o.Id)
                .Take(_batchSize)
                .ToListAsync(ct);

            if (overdueIds.Count == 0)
                break;

            foreach (var orderId in overdueIds)
            {
                try
                {
                    var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
                    if (order == null)
                        continue;
                    if (order.KitchenDelayedNotifiedAt != null || order.IsReadyForDelivery)
                        continue;

                    order.MarkKitchenDelayed(now);
                    _db.OutboxMessages.Add(OutboxMessage.From(new OrderKitchenDelayedEvent
                    {
                        OrderId = order.Id,
                        ExpectedReadyAt = order.ExpectedReadyAt,
                        DetectedAt = now
                    }));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process kitchen delay for order {OrderId}", orderId);
                }
            }

            await _db.SaveChangesAsync(ct);
            batches += 1;
            if (overdueIds.Count < _batchSize)
                break;
        }
    }
}

public interface IOrderKitchenDelayJob
{
    Task ExecuteAsync(CancellationToken ct);
}
