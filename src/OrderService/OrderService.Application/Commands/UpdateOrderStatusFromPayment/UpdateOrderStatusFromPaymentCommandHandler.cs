using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrderStatusFromPayment;

public class UpdateOrderStatusFromPaymentCommandHandler
    : IRequestHandler<UpdateOrderStatusFromPaymentCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<UpdateOrderStatusFromPaymentCommandHandler> _logger;

    public UpdateOrderStatusFromPaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper,
        ILogger<UpdateOrderStatusFromPaymentCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(UpdateOrderStatusFromPaymentCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var order = await uow.Orders.GetOrderByIdAsync(request.OrderId, ct);
        if (order == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Order not found");

        if (order.Status is OrderStatus.Delivered or OrderStatus.InDelivery or OrderStatus.Assigned or OrderStatus.Assigning)
        {
            _logger.LogInformation(
                "Skipping payment status update for order {OrderId}. Current status {Status}, target {TargetStatus}, reason {Reason}",
                request.OrderId,
                order.Status,
                request.NewStatus,
                request.Reason);
            return ApiResponse.SuccessResponse();
        }

        if (order.Status == OrderStatus.Pending &&
            request.NewStatus is OrderStatus.Confirmed or OrderStatus.Failed or OrderStatus.Cancelled)
        {
            _logger.LogWarning(
                "Ignoring payment status update before stock reservation for order {OrderId}. Current status {Status}, target {TargetStatus}, reason {Reason}",
                request.OrderId,
                order.Status,
                request.NewStatus,
                request.Reason);
            return ApiResponse.SuccessResponse();
        }

        if (order.Status == request.NewStatus)
            return ApiResponse.SuccessResponse();

        order.ChangeStatus(request.NewStatus);

        var outboxMessages = order.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        return ApiResponse.SuccessResponse();
    }
}
