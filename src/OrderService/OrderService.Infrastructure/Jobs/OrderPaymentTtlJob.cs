using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Aggregates;
using OrderService.Infrastructure.Persistence;
using Shared.Contracts;

namespace OrderService.Infrastructure.Jobs;

public sealed class OrderPaymentTtlJob : IOrderPaymentTtlJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOrderIntegrationEventMapper _mapper;
    private readonly ILogger<OrderPaymentTtlJob> _logger;
    private readonly TimeSpan _ttl;
    private readonly int _batchSize;
    private readonly int _maxBatches;

    public OrderPaymentTtlJob(
        IOrderIntegrationEventMapper mapper,
        IConfiguration config,
        ILogger<OrderPaymentTtlJob> logger, IServiceScopeFactory scopeFactory)
    {
        _mapper = mapper;
        _logger = logger;
        _scopeFactory = scopeFactory;

        var ttlMinutes = int.TryParse(config["Order:PaymentTtlMinutes"], out var ttlValue) ? ttlValue : 15;
        _batchSize = int.TryParse(config["Order:PaymentTtlBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Order:PaymentTtlMaxBatches"], out var maxValue) ? maxValue : 20;
        _ttl = ttlMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (_ttl == TimeSpan.Zero)
        {
            _logger.LogInformation("Order payment TTL is disabled.");
            return;
        }

        var cutoff = DateTime.UtcNow.Subtract(_ttl);
        var batches = 0;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        while (!ct.IsCancellationRequested && batches < _maxBatches)
        {
            var expiredOrderIds = await db.Orders.AsNoTracking()
                .Where(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Reserved) && o.CreatedAt < cutoff)
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
                    if (order.Status is not (OrderStatus.Pending or OrderStatus.Reserved))
                        continue;

                    order.Cancel("payment_ttl_expired");

                    var outboxMessages = order.DomainEvents
                        .Select(_mapper.MapFromDomainEvent)
                        .Where(ie => ie != null)
                        .Select(OutboxMessage.From!)
                        .ToList();
                    if(outboxMessages.Count != 0) db.OutboxMessages.AddRange(outboxMessages);
                    
                    await db.SaveChangesAsync(ct);
                    order.ClearDomainEvents();

                    _logger.LogInformation("Order {OrderId} canceled due to payment TTL", orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cancel expired order {OrderId}", orderId);
                }
            }

            batches += 1;
            if (expiredOrderIds.Count < _batchSize)
                break;
        }
    }
}

public interface IOrderPaymentTtlJob
{
    Task ExecuteAsync(CancellationToken ct);
}
