using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderRejected;

public sealed class MarkOrderRejectedCommandHandler : IRequestHandler<MarkOrderRejectedCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<MarkOrderRejectedCommandHandler> _logger;

    public MarkOrderRejectedCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<MarkOrderRejectedCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(MarkOrderRejectedCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        if (order.Status is OrderStatus.Delivered or OrderStatus.InDelivery or OrderStatus.Assigned)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Order is already in delivery");

        order.RejectByKitchen(request.Reason);

        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order {OrderId} rejected by kitchen", order.Id);
        return ApiResponse.SuccessResponse();
    }
}
