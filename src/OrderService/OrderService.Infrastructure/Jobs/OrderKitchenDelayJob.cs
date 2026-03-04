using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.Jobs;

public sealed class OrderKitchenDelayJob : IOrderKitchenDelayJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderKitchenDelayJob> _logger;
    private readonly int _batchSize;
    private readonly int _maxBatches;

    public OrderKitchenDelayJob(
        IConfiguration config,
        ILogger<OrderKitchenDelayJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _batchSize = int.TryParse(config["Order:KitchenDelayBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Order:KitchenDelayMaxBatches"], out var maxValue) ? maxValue : 20;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var batches = 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        while (!ct.IsCancellationRequested && batches < _maxBatches)
        {
            var overdueIds = await db.Orders.AsNoTracking()
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
                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
                    if (order == null)
                        continue;
                    if (order.KitchenDelayedNotifiedAt != null || order.IsReadyForDelivery)
                        continue;

                    order.MarkKitchenDelayed(now);
                    db.OutboxMessages.Add(OutboxMessage.From(new OrderKitchenDelayedEvent
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

            await db.SaveChangesAsync(ct);
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
