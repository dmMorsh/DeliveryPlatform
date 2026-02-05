using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.Events;
using MediatR;
using Shared.Contracts.Events;
using Shared.Utilities;

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
            return ApiResponse.ErrorResponse("No item in request");

        var shardGroups = request.ReleaseStockModels
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            var success = await ProcessMessage(shardId, request.OrderId, shardGroup.ToArray(), ct);
            if (!success)
                return ApiResponse.ErrorResponse("Release failed");
        }
        
        return ApiResponse.SuccessResponse("Stock released");
    }

    private async Task<bool> ProcessMessage(int shardId, Guid orderId, SimpleStockItemModel[] releaseStockModels, CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId); 
        
        var reservations = await uow.Reservations.GetActiveReservationsAsync(orderId, ct);
        if (!reservations.Any())
            return true;
            
        var outboxMessages = new List<OutboxMessage>();
        var failedItems = new List<FailedStockItemSnapshot>();
        var toRelease = new List<(StockItem stock, int qty)>();
        
        foreach (var releaseStockModel in releaseStockModels)
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
            var releaseFailedEvent = _eventMapper.MapStockReleaseFailedEvent(orderId, failedItems);
            outboxMessages.Add(OutboxMessage.From(releaseFailedEvent));
            await uow.SaveChangesAsync(outboxMessages, ct);
            return false;
        }
        
        foreach (var (stock, quantity) in toRelease)
        {
            stock.Release(quantity, orderId, checkAvailability: false);
        }
        foreach (var stockReservation in reservations)
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
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        
        foreach (var item in toRelease.Select(x=>x.stock)) 
            item.ClearDomainEvents();

        return true;
    }
}