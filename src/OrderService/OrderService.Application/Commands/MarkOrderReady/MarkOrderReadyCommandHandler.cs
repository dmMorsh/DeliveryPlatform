using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Application.Services;
using OrderService.Domain.Aggregates;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderReady;

public sealed class MarkOrderReadyCommandHandler : IRequestHandler<MarkOrderReadyCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<MarkOrderReadyCommandHandler> _logger;

    public MarkOrderReadyCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<MarkOrderReadyCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(MarkOrderReadyCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Failed)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is canceled or failed");

        if (order.Status is OrderStatus.Assigned or OrderStatus.Assigning or OrderStatus.InDelivery or OrderStatus.Delivered)
            return ApiResponse.SuccessResponse();

        if (order.Status != OrderStatus.Confirmed)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is not confirmed");

        order.MarkReadyForDelivery();

        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();
        OrderReadCache.Invalidate(order.Id);

        _logger.LogInformation("Order {OrderId} marked as ready for delivery", order.Id);
        return ApiResponse.SuccessResponse();
    }
}
