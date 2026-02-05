using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;
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
            return ApiResponse.ErrorResponse("No item in request");

        var shardGroups = request.ReserveStockModels
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            var success = await ProcessMessage(shardId, request.OrderId, shardGroup.ToArray(), ct);
            if (!success)
                return ApiResponse.ErrorResponse("Reservation failed");
        }
        
        return ApiResponse.SuccessResponse("item reserved");
    }

    private async Task<bool> ProcessMessage(int shardId, Guid orderId, SimpleStockItemModel[] reserveStockModels,
        CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId);

        if (await uow.Reservations.ReservationExistAsync(orderId, ct))
            return true;

        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toReserve = new List<(StockItem stock, int qty)>();
        
        foreach (var reserveStockModel in reserveStockModels)
        {
            var stock = await uow.Stock
                .GetByProductIdAsync(reserveStockModel.ProductId, ct);
            if (stock == null)
            {
                failedItems.Add(new FailedStockItemSnapshot
                    {
                        ProductId = reserveStockModel.ProductId,
                        Reason = "Stock item not found",
                        Quantity = reserveStockModel.Quantity
                    }
                );
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
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        
        foreach (var item in toReserve.Select(x=>x.stock)) 
            item.ClearDomainEvents();
        
        return true;
    }
}