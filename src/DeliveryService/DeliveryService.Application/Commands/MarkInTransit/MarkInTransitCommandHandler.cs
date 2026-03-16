using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.MarkInTransit;

public class MarkInTransitCommandHandler : IRequestHandler<MarkInTransitCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;

    public MarkInTransitCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(MarkInTransitCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        if (delivery.CourierId != request.CourierId)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Courier mismatch");

        if (delivery.Status is DeliveryStatus.InDelivery or DeliveryStatus.Delivered)
            return ApiResponse.SuccessResponse();

        if (delivery.Status != DeliveryStatus.PickedUp)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Delivery is not picked up");

        delivery.MarkInTransit();

        var outbox = delivery.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(e => e != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outbox, ct);
        delivery.ClearDomainEvents();
        DeliveryReadCache.Invalidate(delivery.Id, delivery.OrderId);

        return ApiResponse.SuccessResponse();
    }
}
