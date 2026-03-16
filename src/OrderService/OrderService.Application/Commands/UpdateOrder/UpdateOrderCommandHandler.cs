using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using Shared.Contracts;
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
            return ApiResponse<OrderView>.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        var oldStatus = order.Status;

        if (request.Status.HasValue)
            order.ChangeStatus(request.Status.Value);

        if (request.CourierId.HasValue)
            order.AssignCourier(request.CourierId.Value, request.CourierName);

        if (!string.IsNullOrWhiteSpace(request.CourierNote))
            order.AddCourierNote(request.CourierNote);
        
        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order updated: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);
        
        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>());
    }
}