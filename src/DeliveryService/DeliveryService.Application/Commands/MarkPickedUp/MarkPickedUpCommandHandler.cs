using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.MarkPickedUp;

public class MarkPickedUpCommandHandler : IRequestHandler<MarkPickedUpCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;

    public MarkPickedUpCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(MarkPickedUpCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        if (delivery.CourierId != request.CourierId)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Courier mismatch");

        if (delivery.Status is DeliveryStatus.PickedUp or DeliveryStatus.InDelivery or DeliveryStatus.Delivered)
            return ApiResponse.SuccessResponse();

        if (delivery.Status != DeliveryStatus.Assigned)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Delivery is not assigned");

        delivery.MarkPickedUp();

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
