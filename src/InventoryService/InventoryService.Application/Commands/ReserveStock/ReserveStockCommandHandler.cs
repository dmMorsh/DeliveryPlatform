using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using Mapster;
using MediatR;
using Shared.Contracts.Events;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public class ReserveStockCommandHandler
    : IRequestHandler<ReserveStockCommand, ApiResponse<List<StockView>>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IStockIntegrationEventMapper _eventMapper;

    public ReserveStockCommandHandler(
        IUnitOfWorkFactory factory,
        IStockIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<List<StockView>>> Handle(
        ReserveStockCommand request,
        CancellationToken ct)
    {
        if (request.ReserveStockModels.Length == 0)
            return ApiResponse<List<StockView>>.ErrorResponse("No item in request");

        await using var uow = _factory.Create(request.ReserveStockModels.First().ProductId);
        
        if (await uow.Reservations.ReservationExistAsync(request.OrderId, ct))
            return ApiResponse<List<StockView>>.ErrorResponse("Reservation already exist");

        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toReserve = new List<(StockItem stock, int qty)>();
        
        foreach (var reserveStockModel in request.ReserveStockModels)
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
            var reserveFailedEvent = _eventMapper.MapStockReserveFailedEvent(request.OrderId, failedItems);
            outboxMessages.Add(OutboxMessage.From(reserveFailedEvent));
            await uow.SaveChangesAsync(outboxMessages, ct);
            return ApiResponse<List<StockView>>.ErrorResponse("Reservation failed");
        }

        foreach (var (stock, quantity) in toReserve)
        {
            stock.Reserve(quantity, request.OrderId, checkAvailability: false);
            await uow.Reservations.AddReservationAsync(new StockReservation
            {
                OrderId = request.OrderId,
                ProductId = stock.ProductId,
                Quantity = quantity
            }, ct);
        }
        
        var integrationEvent = _eventMapper.MapStockReservedEvent(
            request.OrderId,
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
        
        return ApiResponse<List<StockView>>.SuccessResponse(toReserve.Select(x=>x.stock).Adapt<List<StockView>>(), "item reserved");
    }
}