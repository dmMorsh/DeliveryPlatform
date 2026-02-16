using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrder;

public class UpdateOrderCommandHandler
    : IRequestHandler<UpdateOrderCommand, ApiResponse<OrderView>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<UpdateOrderCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderView>> Handle(
        UpdateOrderCommand request,
        CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse<OrderView>.ErrorResponse("Order not found");

        var oldStatus = order.Status;

        if (request.Status.HasValue)
            order.ChangeStatus(request.Status.Value);

        if (request.CourierId.HasValue)
            order.AssignCourier(request.CourierId.Value);

        if (!string.IsNullOrWhiteSpace(request.CourierNote))
            order.AddCourierNote(request.CourierNote);
        
        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        // Add status changed event if status was modified
        if (request.Status.HasValue && oldStatus != order.Status)
        {
            var statusChangeEvent = _eventMapper.MapOrderStatusChangedEvent(order, oldStatus, order.Status);
            outboxMessages.Add(OutboxMessage.From(statusChangeEvent));
        }

        // Add order assigned event if courier was assigned
        if (request.CourierId.HasValue && order.CourierId == request.CourierId.Value)
        {
            var assignedEvent = _eventMapper.MapOrderAssignedEvent(order, request.CourierId.Value, request.CourierName ?? "Unknown");
            outboxMessages.Add(OutboxMessage.From(assignedEvent));
        }

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order updated: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);
        
        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>());
    }
}