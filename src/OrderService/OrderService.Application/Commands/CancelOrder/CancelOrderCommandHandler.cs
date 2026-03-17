using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Aggregates;
using Shared.Contracts;

namespace OrderService.Application.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Failed)
            return ApiResponse.SuccessResponse();

        if (order.Status is OrderStatus.InDelivery or OrderStatus.Delivered)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is already in delivery");

        if (order.IsReadyForDelivery)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is already prepared");

        try
        {
            order.Cancel(request.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel order {OrderId}", request.OrderId);
            return ApiResponse.ErrorResponse("Order cannot be canceled");
        }

        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order canceled: {OrderId}", order.Id);
        return ApiResponse.SuccessResponse();
    }
}
