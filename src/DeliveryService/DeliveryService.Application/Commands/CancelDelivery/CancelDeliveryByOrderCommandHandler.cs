using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CancelDelivery;

public class CancelDeliveryByOrderCommandHandler : IRequestHandler<CancelDeliveryByOrderCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;

    public CancelDeliveryByOrderCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(CancelDeliveryByOrderCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (delivery == null)
            return ApiResponse.SuccessResponse();

        if (delivery.Status == DeliveryStatus.Delivered)
            return ApiResponse.SuccessResponse();

        if (delivery.Status is DeliveryStatus.Cancelled or DeliveryStatus.Failed or DeliveryStatus.Returned)
            return ApiResponse.SuccessResponse();

        delivery.Cancel(request.Reason);

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
