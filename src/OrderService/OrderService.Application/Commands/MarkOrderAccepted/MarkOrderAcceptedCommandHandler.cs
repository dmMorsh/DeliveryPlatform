using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Application.Services;
using OrderService.Domain.Aggregates;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderAccepted;

public sealed class MarkOrderAcceptedCommandHandler : IRequestHandler<MarkOrderAcceptedCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<MarkOrderAcceptedCommandHandler> _logger;

    public MarkOrderAcceptedCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<MarkOrderAcceptedCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(MarkOrderAcceptedCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Failed)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is canceled or failed");

        order.AcceptByKitchen();

        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();
        OrderReadCache.Invalidate(order.Id);

        _logger.LogInformation("Order {OrderId} accepted by kitchen", order.Id);
        return ApiResponse.SuccessResponse();
    }
}
