using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.FailDelivery;

public class FailDeliveryCommandHandler : IRequestHandler<FailDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;

    public FailDeliveryCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(FailDeliveryCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse("Delivery not found");

        delivery.Fail(request.Reason);

        var outbox = delivery.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(e => e != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outbox, ct);
        delivery.ClearDomainEvents();

        return ApiResponse.SuccessResponse();
    }
}
