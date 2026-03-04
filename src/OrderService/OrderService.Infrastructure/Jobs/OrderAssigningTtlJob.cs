using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderService.Infrastructure.Jobs;

public sealed class OrderAssigningTtlJob : IOrderAssigningTtlJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOrderIntegrationEventMapper _mapper;
    private readonly ILogger<OrderAssigningTtlJob> _logger;
    private readonly TimeSpan _ttl;
    private readonly int _batchSize;
    private readonly int _maxBatches;

    public OrderAssigningTtlJob(
        IOrderIntegrationEventMapper mapper,
        IConfiguration config,
        ILogger<OrderAssigningTtlJob> logger, IServiceScopeFactory scopeFactory)
    {
        _mapper = mapper;
        _logger = logger;
        _scopeFactory = scopeFactory;

        var ttlMinutes = int.TryParse(config["Order:AssigningTtlMinutes"], out var ttlValue) ? ttlValue : 30;
        _batchSize = int.TryParse(config["Order:AssigningTtlBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Order:AssigningTtlMaxBatches"], out var maxValue) ? maxValue : 20;
        _ttl = ttlMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (_ttl == TimeSpan.Zero)
        {
            _logger.LogInformation("Order assigning TTL is disabled.");
            return;
        }

        var cutoff = DateTime.UtcNow.Subtract(_ttl);
        var batches = 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        while (!ct.IsCancellationRequested && batches < _maxBatches)
        {
            var expiredOrderIds = await db.Orders.AsNoTracking()
                .Where(o => o.Status == OrderStatus.Assigning
                            && (o.IsReadyForDelivery ? o.ReadyAt < cutoff : o.CreatedAt < cutoff))
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.Id)
                .Take(_batchSize)
                .ToListAsync(ct);

            if (expiredOrderIds.Count == 0)
                break;

            foreach (var orderId in expiredOrderIds)
            {
                try
                {
                    var order = await db.Orders.FindAsync(orderId, ct);
                    if (order == null)
                        continue;
                    if (order.Status != OrderStatus.Assigning)
                        continue;

                    var outbox = new List<OutboxMessage>();

                    if (order.IsReadyForDelivery)
                    {
                        outbox.Add(OutboxMessage.From(new OrderAssigningTimeoutEvent
                        {
                            OrderId = order.Id,
                            ReadyAt = order.ReadyAt,
                            DetectedAt = DateTime.UtcNow
                        }));
                    }
                    else
                    {
                        order.Cancel("assigning_ttl_expired");
                        outbox.AddRange(order.DomainEvents
                            .Select(_mapper.MapFromDomainEvent)
                            .Where(ie => ie != null)
                            .Select(OutboxMessage.From!));
                        order.ClearDomainEvents();
                    }

                    if (outbox.Count > 0)
                    {
                        db.OutboxMessages.AddRange(outbox);
                        await db.SaveChangesAsync(ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process assigning TTL for order {OrderId}", orderId);
                }
            }

            batches += 1;
            if (expiredOrderIds.Count < _batchSize)
                break;
        }
    }
}

public interface IOrderAssigningTtlJob
{
    Task ExecuteAsync(CancellationToken ct);
}
