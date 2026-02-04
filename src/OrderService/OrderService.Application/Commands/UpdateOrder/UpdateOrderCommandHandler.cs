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
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<UpdateOrderCommandHandler> _logger;

    public UpdateOrderCommandHandler(
        IOrderRepository orders,
        IUnitOfWork uow,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<UpdateOrderCommandHandler> logger)
    {
        _orders = orders;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderView>> Handle(
        UpdateOrderCommand request,
        CancellationToken ct)
    {
        var order = await _orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse<OrderView>.ErrorResponse("Order not found");

        var dto = request.Model;
        var oldStatus = order.Status;

        if (dto.Status.HasValue)
            order.ChangeStatus(dto.Status.Value);

        if (dto.CourierId.HasValue)
            order.AssignCourier(dto.CourierId.Value);

        if (!string.IsNullOrWhiteSpace(dto.CourierNote))
            order.AddCourierNote(dto.CourierNote);
        
        var outboxMessages = order.DomainEvents
            .Select(de =>
            {
                var ie = _eventMapper.MapFromDomainEvent(de);
                return OutboxMessage.From(ie!);
            })
            .ToList();

        // Add status changed event if status was modified
        if (dto.Status.HasValue && oldStatus != order.Status)
        {
            var statusChangeEvent = _eventMapper.MapOrderStatusChangedEvent(order, (int)oldStatus, (int)order.Status);
            outboxMessages.Add(OutboxMessage.From(statusChangeEvent));
        }

        // Add order assigned event if courier was assigned
        if (dto.CourierId.HasValue && order.CourierId == dto.CourierId.Value)
        {
            var assignedEvent = _eventMapper.MapOrderAssignedEvent(order, dto.CourierId.Value, dto.CourierName ?? "Unknown");
            outboxMessages.Add(OutboxMessage.From(assignedEvent));
        }

        await _uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>());
    }
}