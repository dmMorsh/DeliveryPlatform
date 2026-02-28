using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Jobs;

public sealed class InventoryReservationAlertJob : IInventoryReservationAlertJob
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IStockIntegrationEventMapper _eventMapper;
    private readonly ILogger<InventoryReservationAlertJob> _logger;
    private readonly int _batchSize;
    private readonly int _maxBatches;
    private readonly TimeSpan _ttl;
    private readonly int _shardCount;

    public InventoryReservationAlertJob(
        IUnitOfWorkFactory uowFactory,
        IStockIntegrationEventMapper eventMapper,
        IConfiguration config,
        ILogger<InventoryReservationAlertJob> logger)
    {
        _uowFactory = uowFactory;
        _eventMapper = eventMapper;
        _logger = logger;

        var ttlMinutes = int.TryParse(config["Inventory:ReservationAlertTtlMinutes"], out var ttlValue) ? ttlValue : 120;
        _batchSize = int.TryParse(config["Inventory:ReservationAlertBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Inventory:ReservationAlertMaxBatches"], out var maxValue) ? maxValue : 20;
        _shardCount = int.TryParse(config["ShardCount"], out var shardValue) ? shardValue : 1;
        _ttl = ttlMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(ttlMinutes);
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (_ttl == TimeSpan.Zero)
        {
            _logger.LogInformation("Inventory reservation alert TTL is disabled.");
            return;
        }

        var cutoff = DateTime.UtcNow.Subtract(_ttl);

        for (var shardId = 0; shardId < _shardCount; shardId++)
        {
            var batches = 0;
            while (!ct.IsCancellationRequested && batches < _maxBatches)
            {
                await using var uow = _uowFactory.Create(shardId);
                var staleOrderIds = await uow.Reservations.GetStaleOrderIdsAsync(cutoff, _batchSize, ct);
                if (staleOrderIds.Count == 0)
                    break;

                var outbox = new List<OutboxMessage>();

                foreach (var orderId in staleOrderIds)
                {
                    var reservations = await uow.Reservations.GetActiveReservationsAsync(orderId, ct);
                    if (reservations.Count == 0)
                        continue;

                    var oldest = reservations.Min(r => r.CreatedAt);
                    var items = reservations
                        .Select(r => new StockItemSnapshot
                        {
                            ProductId = r.ProductId,
                            Quantity = r.Quantity
                        })
                        .ToArray();

                    var evt = _eventMapper.MapStockReservationStaleDetectedEvent(orderId, oldest, items);
                    outbox.Add(OutboxMessage.From(evt));
                }

                if (outbox.Count > 0)
                {
                    await uow.SaveChangesAsync(outbox, ct);
                    _logger.LogInformation("Inventory reservation alert emitted for {Count} orders (shard {Shard})",
                        outbox.Count, shardId);
                }

                batches += 1;
                if (staleOrderIds.Count < _batchSize)
                    break;
            }
        }
    }
}

public interface IInventoryReservationAlertJob
{
    Task ExecuteAsync(CancellationToken ct);
}
