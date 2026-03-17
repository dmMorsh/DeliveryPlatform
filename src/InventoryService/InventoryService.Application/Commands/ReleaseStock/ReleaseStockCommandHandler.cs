using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace InventoryService.Application.Commands.ReleaseStock;

public class ReleaseStockCommandHandler
    : IRequestHandler<ReleaseStockCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IStockIntegrationEventMapper _eventMapper;
    private readonly IShardResolver _resolver;

    public ReleaseStockCommandHandler(
        IUnitOfWorkFactory factory,
        IStockIntegrationEventMapper eventMapper, IShardResolver resolver)
    {
        _eventMapper = eventMapper;
        _resolver = resolver;
        _factory = factory;
    }

    public async Task<ApiResponse> Handle(
        ReleaseStockCommand request,
        CancellationToken ct)
    {
        if (request.ReleaseStockModels.Length == 0)
            return ApiResponse.ErrorResponse(ErrorCodes.Validation, "No item in request");

        var shardGroups = request.ReleaseStockModels
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            var success = await ProcessMessage(shardId, request.OrderId, shardGroup.ToArray(), ct);
            if (!success)
                return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Release failed");
        }
        
        return ApiResponse.SuccessResponse("Stock released");
    }

    private async Task<bool> ProcessMessage(int shardId, Guid orderId, SimpleStockItemModel[] releaseStockModels, CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId); 
        
        var reservations = await uow.Reservations.GetActiveReservationsAsync(orderId, ct);
        if (!reservations.Any())
            return true;

        var reservationsByProduct = reservations
            .GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toRelease = new List<(StockItem stock, int qty)>();
        var reservationsToRelease = new List<StockReservation>();
        
        var productIds = releaseStockModels
            .Where(m => reservationsByProduct.ContainsKey(m.ProductId))
            .Select(m => m.ProductId)
            .Distinct()
            .ToList();
        var stockItems = productIds.Count == 0
            ? []
            : await uow.Stock.GetByProductIdsAsync(productIds, ct);
        var stockById = stockItems.ToDictionary(s => s.Id);

        foreach (var releaseStockModel in releaseStockModels)
        {
            if (!reservationsByProduct.TryGetValue(releaseStockModel.ProductId, out var reservation))
                continue;

            if (!stockById.TryGetValue(releaseStockModel.ProductId, out var stock))
            {
                failedItems.Add(new FailedStockItemSnapshot
                {
                    ProductId = releaseStockModel.ProductId,
                    Reason = "Stock item not found",
                    Quantity = releaseStockModel.Quantity
                });
                continue;
            }
            
            var error = stock.CanRelease(reservation.Quantity);
            if (error != null)
            {
                failedItems.Add(new FailedStockItemSnapshot
                {
                    ProductId = releaseStockModel.ProductId,
                    Reason = error,
                    Quantity = reservation.Quantity,
                });
                continue;
            }
                       
            toRelease.Add((stock, reservation.Quantity));
            reservationsToRelease.Add(reservation);
        }
        
        if (failedItems.Count != 0)
        {
            var releaseFailedEvent = _eventMapper.MapStockReleaseFailedEvent(orderId, failedItems);
            outboxMessages.Add(OutboxMessage.From(releaseFailedEvent));
            await uow.SaveChangesAsync(outboxMessages, ct);
            return false;
        }

        if (toRelease.Count == 0)
            return true;
        
        foreach (var (stock, quantity) in toRelease)
        {
            stock.Release(quantity, orderId, checkAvailability: false);
        }
        foreach (var stockReservation in reservationsToRelease)
        {
            stockReservation.ReleasedAt = DateTime.UtcNow;
        }
            
        var integrationEvent = _eventMapper.MapStockReleasedEvent(
            orderId,
            toRelease.Select(x=>x.stock).SelectMany(si => si.DomainEvents)
                .OfType<StockReleasedDomainEvent>()
                .Select(di => new StockItemSnapshot
                {
                    ProductId = di.ProductId,
                    Quantity = di.Quantity,
                }).ToArray()
        );
        outboxMessages.Add(OutboxMessage.From(integrationEvent));

        var quantityChangedEvents = toRelease
            .Select(x => _eventMapper.MapStockQuantityChangedEvent(
                x.stock.Id,
                x.stock.TotalQuantity,
                x.stock.ReservedQuantity,
                x.stock.AvailableQuantity))
            .Select(OutboxMessage.From);
        outboxMessages.AddRange(quantityChangedEvents);
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        
        foreach (var item in toRelease.Select(x=>x.stock)) 
            item.ClearDomainEvents();

        return true;
    }
}
