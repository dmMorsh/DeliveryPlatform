using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.DeclineDelivery;

public class DeclineDeliveryCommandHandler : IRequestHandler<DeclineDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IAssignmentService _assignmentService;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<DeclineDeliveryCommandHandler> _logger;

    public DeclineDeliveryCommandHandler(
        IDeliveryRepository repository,
        IAssignmentService assignmentService,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<DeclineDeliveryCommandHandler> logger)
    {
        _repository = repository;
        _assignmentService = assignmentService;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(DeclineDeliveryCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse("Delivery not found");

        delivery.DeclineOffer(request.CourierId, request.Reason);
        var offered = await _assignmentService.OfferNextCourierAsync(delivery, ct);
        if (!offered)
            _logger.LogWarning("No available couriers after decline for delivery {DeliveryId}", delivery.Id);
        
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
