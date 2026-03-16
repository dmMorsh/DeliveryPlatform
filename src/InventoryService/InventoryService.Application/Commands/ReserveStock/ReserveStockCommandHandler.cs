using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Contracts.Events;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public class ReserveStockCommandHandler
    : IRequestHandler<ReserveStockCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IStockIntegrationEventMapper _eventMapper;
    private readonly IShardResolver _resolver;

    public ReserveStockCommandHandler(
        IUnitOfWorkFactory factory,
        IStockIntegrationEventMapper eventMapper, 
        IShardResolver resolver)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _resolver = resolver;
    }

    public async Task<ApiResponse> Handle(
        ReserveStockCommand request,
        CancellationToken ct)
    {
        if (request.ReserveStockModels.Length == 0)
            return ApiResponse.ErrorResponse(ErrorCodes.Validation, "No item in request");

        var shardGroups = request.ReserveStockModels
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            var success = await ProcessMessage(shardId, request.OrderId, shardGroup.ToArray(), ct);
            if (!success)
                return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Reservation failed");
        }
        
        return ApiResponse.SuccessResponse("item reserved");
    }

    private async Task<bool> ProcessMessage(int shardId, Guid orderId, SimpleStockItemModel[] reserveStockModels,
        CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId);

        var reservedIds = await uow.Reservations.GetReservedProductIdsAsync(
            orderId,
            reserveStockModels.Select(m => m.ProductId),
            ct);
        if (reservedIds.Count > 0)
        {
            reserveStockModels = reserveStockModels
                .Where(m => !reservedIds.Contains(m.ProductId))
                .ToArray();
        }
        if (reserveStockModels.Length == 0)
            return true;

        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toReserve = new List<(StockItem stock, int qty)>();
        
        var productIds = reserveStockModels.Select(m => m.ProductId).ToList();
        var stockItems = await uow.Stock.GetByProductIdsAsync(productIds, ct);
        var stockById = stockItems.ToDictionary(s => s.Id);

        foreach (var reserveStockModel in reserveStockModels)
        {
            if (!stockById.TryGetValue(reserveStockModel.ProductId, out var stock))
            {
                failedItems.Add(new FailedStockItemSnapshot
                {
                    ProductId = reserveStockModel.ProductId,
                    Reason = "Stock item not found",
                    Quantity = reserveStockModel.Quantity
                });
                continue;
            }

            var error = stock.CanReserve(reserveStockModel.Quantity);
            if (error != null)
            {
                failedItems.Add(new FailedStockItemSnapshot
                {
                    ProductId = reserveStockModel.ProductId,
                    Reason = error,
                    Quantity = reserveStockModel.Quantity,
                });
                continue;
            }
                
            toReserve.Add((stock, reserveStockModel.Quantity));
        }

        if (failedItems.Count != 0)
        {
            var reserveFailedEvent = _eventMapper.MapStockReserveFailedEvent(orderId, failedItems);
            outboxMessages.Add(OutboxMessage.From(reserveFailedEvent));
            await uow.SaveChangesAsync(outboxMessages, ct);
            return false;
        }

        foreach (var (stock, quantity) in toReserve)
        {
            stock.Reserve(quantity, orderId, checkAvailability: false);
            await uow.Reservations.AddReservationAsync(new StockReservation
            {
                OrderId = orderId,
                ProductId = stock.Id,
                Quantity = quantity
            }, ct);
        }
        
        var integrationEvent = _eventMapper.MapStockReservedEvent(
            orderId,
            toReserve.Select(x=>x.stock).SelectMany(si => si.DomainEvents)
                .OfType<StockReservedDomainEvent>()
                .Select(di => new StockItemSnapshot
                {
                    ProductId = di.ProductId,
                    Quantity = di.Quantity,
                }).ToArray()
            );
        outboxMessages.Add(OutboxMessage.From(integrationEvent));

        var quantityChangedEvents = toReserve
            .Select(x => _eventMapper.MapStockQuantityChangedEvent(
                x.stock.Id,
                x.stock.TotalQuantity,
                x.stock.ReservedQuantity,
                x.stock.AvailableQuantity))
            .Select(OutboxMessage.From);
        outboxMessages.AddRange(quantityChangedEvents);
        
        try
        {
            await uow.SaveChangesAsync(outboxMessages, ct);
        }
        catch (DbUpdateException)
        {
            var guids = toReserve.Select(x => x.stock.Id).ToArray();
            var alreadyReserved = await uow.Reservations.GetReservedProductIdsAsync(orderId, guids, ct);
            if (alreadyReserved.Count == guids.Length)
                return true;
            throw;
        }
        
        foreach (var item in toReserve.Select(x=>x.stock)) 
            item.ClearDomainEvents();
        
        return true;
    }
}
