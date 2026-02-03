using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Events;
using MediatR;
using Shared.Contracts.Events;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReleaseStock;

public class ReleaseStockCommandHandler
    : IRequestHandler<ReleaseStockCommand, ApiResponse<Unit>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IStockIntegrationEventMapper _eventMapper;

    public ReleaseStockCommandHandler(
        IUnitOfWorkFactory factory,
        IStockIntegrationEventMapper eventMapper)
    {
        _eventMapper = eventMapper;
        _factory = factory;
    }

    public async Task<ApiResponse<Unit>> Handle(
        ReleaseStockCommand request,
        CancellationToken ct)
    {
        // if (request.ReleaseStockModels?.Length == 0)
        //     return ApiResponse<Unit>.ErrorResponse("No item in request");

        await using var uow = request.ReleaseStockModels != null && request.ReleaseStockModels.Any()
            ? _factory.Create(request.ReleaseStockModels.First().ProductId) 
            : _factory.Create(request.ShardId);
        
        var reservations = await uow.Reservations.GetActiveReservationsAsync(request.OrderId, ct);
        if (!reservations.Any())
            return ApiResponse<Unit>.ErrorResponse("No item reserved");
            
        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toRelease = new List<(StockItem stock, int qty)>();
        
        foreach (var releaseStockModel in reservations)
        {
            var stock = await uow.Stock
                .GetByProductIdAsync(releaseStockModel.ProductId, ct);
            if (stock == null)
            {
                failedItems.Add(new FailedStockItemSnapshot
                    {
                        ProductId = releaseStockModel.ProductId,
                        Reason = "Stock item not found",
                        Quantity = releaseStockModel.Quantity
                    }
                );
                continue;
            }
            
            var error = stock.CanRelease(releaseStockModel.Quantity);
            if (error != null)
            {
                failedItems.Add(new FailedStockItemSnapshot
                {
                    ProductId = releaseStockModel.ProductId,
                    Reason = error,
                    Quantity = releaseStockModel.Quantity,
                });
                continue;
            }
                       
            toRelease.Add((stock, releaseStockModel.Quantity));
        }
        
        if (failedItems.Count != 0)
        {
            var releaseFailedEvent = _eventMapper.MapStockReleaseFailedEvent(request.OrderId, failedItems);
            outboxMessages.Add(OutboxMessage.From(releaseFailedEvent));
            await uow.SaveChangesAsync(outboxMessages, ct);
            return ApiResponse<Unit>.ErrorResponse("Release failed");
        }
        
        foreach (var (stock, quantity) in toRelease)
        {
            stock.Release(quantity, request.OrderId, checkAvailability: false);
        }
        foreach (var stockReservation in reservations)
        {
            stockReservation.ReleasedAt = DateTime.UtcNow;
        }
            
        var integrationEvent = _eventMapper.MapStockReleasedEvent(
            request.OrderId,
            toRelease.Select(x=>x.stock).SelectMany(si => si.DomainEvents)
                .OfType<StockReleasedDomainEvent>()
                .Select(di => new StockItemSnapshot
                {
                    ProductId = di.ProductId,
                    Quantity = di.Quantity,
                }).ToArray()
        );
        outboxMessages.Add(OutboxMessage.From(integrationEvent));
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        
        foreach (var item in toRelease.Select(x=>x.stock)) 
            item.ClearDomainEvents();
        
        return ApiResponse<Unit>.SuccessResponse(Unit.Value, "Stock released");
    }
}