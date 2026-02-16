using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
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
            return ApiResponse.ErrorResponse("Delivery not found");

        if (delivery.CourierId != request.CourierId)
            return ApiResponse.ErrorResponse("Courier mismatch");

        delivery.MarkPickedUp();

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
