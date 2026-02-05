using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkStockReservationFailed;

public class MarkStockReservationFailedCommandHandler : IRequestHandler<MarkStockReservationFailedCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<MarkStockReservationFailedCommandHandler> _logger;

    public MarkStockReservationFailedCommandHandler(IUnitOfWorkFactory factory, IOrderIntegrationEventMapper eventMapper, ILogger<MarkStockReservationFailedCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(MarkStockReservationFailedCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse("Order not found");

        if (order.Status is not (OrderStatus.Pending 
            or OrderStatus.Reserved // ? 
            or OrderStatus.Failed 
            or OrderStatus.Cancelled))
        {
            _logger.LogInformation(
                "Order {OrderId} already processed, status = {Status}",
                order.Id,
                order.Status);

            return ApiResponse.SuccessResponse();
        }

        var items = order.Items
            .Join<OrderItem, UpdateOrderItemModel, Guid, (OrderItem OrderItem, UpdateOrderItemModel RequestItem)>(
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
       
        order.MarkItemsFailed(items.Select(pair => pair.OrderItem).ToArray());
        
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

    private async Task<bool> HasErrors(IUnitOfWork uow, Order order, 
        (OrderItem OrderItem, UpdateOrderItemModel RequestItem)[] items,
        MarkStockReservationFailedCommand request, CancellationToken ct)
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