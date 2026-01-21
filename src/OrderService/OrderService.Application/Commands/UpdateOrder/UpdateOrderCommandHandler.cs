using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Domain;
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

        if (dto.Status.HasValue)
            order.ChangeStatus((OrderStatus)dto.Status.Value);

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

        await _uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>());
    }
}