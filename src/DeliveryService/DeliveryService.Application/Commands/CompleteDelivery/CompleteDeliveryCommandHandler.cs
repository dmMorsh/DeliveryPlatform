using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CompleteDelivery;

public class CompleteDeliveryCommandHandler : IRequestHandler<CompleteDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;

    public CompleteDeliveryCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(CompleteDeliveryCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        if (delivery.CourierId != request.CourierId)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Courier mismatch");

        if (delivery.Status == DeliveryStatus.Delivered)
            return ApiResponse.SuccessResponse();

        if (delivery.Status != DeliveryStatus.InDelivery)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Delivery is not in transit");

        delivery.Complete(request.Signature, request.PhotoUrl, request.Notes, request.VerificationCode);

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
