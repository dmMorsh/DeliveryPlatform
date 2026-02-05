using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;
using Shared.Contracts.Events;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateReservedStock;

public class UpdateReservedStockCommandHandler : IRequestHandler<UpdateReservedStockCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<UpdateReservedStockCommandHandler> _logger;

    public UpdateReservedStockCommandHandler(IUnitOfWorkFactory factory, IOrderIntegrationEventMapper eventMapper, ILogger<UpdateReservedStockCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(UpdateReservedStockCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse("Order not found");

        // Already canceled
        if (order.Status is OrderStatus.Failed or OrderStatus.Cancelled)
        {
            return await HandleForCanceledOrder(request, ct, order, uow);
        }
        
        if (order.Status is not OrderStatus.Pending)
        {
            _logger.LogInformation(
                "Order {OrderId} already processed, status = {Status}",
                order.Id,
                order.Status);

            return ApiResponse.SuccessResponse();
        }

        var items = order.Items
            .Join(
                request.OrderItemsModel.Items,
                oi => oi.ProductId,
                ri => ri.ProductId,
                (oi, ri) => (OrderItem: oi, RequestItem: ri)
            )
            .ToArray();

        if (await HasErrors(uow, order, items, request, ct))
        {
            return ApiResponse.ErrorResponse("Stock reservation invariant violation error");
        }

        order.MarkItemsReserved(items.Select(pair => pair.OrderItem).ToArray());
        
        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order updated: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);
        
        return ApiResponse.SuccessResponse();
    }

    private async Task<ApiResponse> HandleForCanceledOrder(UpdateReservedStockCommand request, CancellationToken ct, Order order,
        IUnitOfWork uow)
    {
        var productIds = request.OrderItemsModel.Items
            .Select(i => i.ProductId)
            .ToHashSet();

        var items = order.Items
            .Where(oi => productIds.Contains(oi.ProductId))
            .ToArray();
        
        if (items.Length == 0)
        {
            _logger.LogWarning(
                "No matching items for release in order {OrderId}",
                order.Id);

            return ApiResponse.SuccessResponse();
        }
        
        if (items.All(i => i.Status is OrderItemStatus.Releasing or OrderItemStatus.Released))
        {
            _logger.LogWarning("Items already processed");
            return ApiResponse.SuccessResponse();
        }
        
        order.MarkItemsReleasing(items);

        // MarkItemsReleasing() doesn't make OrderItemsReleaseDomainEvent but smth else might happen
        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();
        if (outboxMessages.All(om => om.GetType() != typeof(StockReservationReleaseRequestedEvent)))
        {
            outboxMessages.Add(OutboxMessage.From(new StockReservationReleaseRequestedEvent
            {
                OrderId = request.OrderId,
                Items = request.OrderItemsModel.Items.Select(i => new IntegrationOrderItemSnapshot
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            }));
        }
        else
        {
            _logger.LogWarning("MarkItemsReleasing made OrderItemsReleaseDomainEvent. Contact the developer.");
        }
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();
        
        _logger.LogInformation("Order already canceled: {OrderNumber} (ID: {OrderId})." +
                               " Sending compensation message.", order.OrderNumber, order.Id);
            
        return ApiResponse.SuccessResponse();
    }
    
    private async Task<bool> HasErrors(IUnitOfWork uow, Order order, 
        (OrderItem OrderItem, UpdateOrderItemModel RequestItem)[] items,
        UpdateReservedStockCommand request, CancellationToken ct)
    {
        if (items.Length != request.OrderItemsModel.Items.Count)
        {
            var error = $"Stock reservation invariant violation. OrderId={order.Id}. Details=Items mismatch";
            order.MarkAsInconsistent(error);
            _logger.LogCritical(error);
            var failMessages = order.DomainEvents
                .Select(_eventMapper.MapFromDomainEvent)
                .Where(ie => ie != null)
                .Select(OutboxMessage.From!)
                .ToList();
        
            await uow.SaveChangesAsync(failMessages, ct);
            order.ClearDomainEvents();
            return true;
        }

        if (items.Any(pair => pair.RequestItem.Quantity != pair.OrderItem.Quantity))
        {
            var error = $"Stock reservation invariant violation. OrderId={order.Id}. Details=Reserved quantity doesn't match ordered";
            order.MarkAsInconsistent(error);
            _logger.LogCritical(error);
            var failMessages = order.DomainEvents
                .Select(_eventMapper.MapFromDomainEvent)
                .Where(ie => ie != null)
                .Select(OutboxMessage.From!)
                .ToList();
        
            await uow.SaveChangesAsync(failMessages, ct);
            order.ClearDomainEvents();
            return true;
        }
        return false;
    }
}