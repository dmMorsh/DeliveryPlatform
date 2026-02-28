using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.DeclineDelivery;

public class DeclineDeliveryCommandHandler : IRequestHandler<DeclineDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IAssignmentService _assignmentService;
    private readonly IAssignmentQueue _queue;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<DeclineDeliveryCommandHandler> _logger;

    public DeclineDeliveryCommandHandler(
        IDeliveryRepository repository,
        IAssignmentService assignmentService,
        IAssignmentQueue queue,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<DeclineDeliveryCommandHandler> logger)
    {
        _repository = repository;
        _assignmentService = assignmentService;
        _queue = queue;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(DeclineDeliveryCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        var lastAttempt = delivery.AssignmentAttempts
            .LastOrDefault(a => a.CourierId == request.CourierId);

        if (delivery.Status != DeliveryStatus.Assigning)
        {
            if (lastAttempt?.Status is DeliveryAssignmentStatus.Declined or DeliveryAssignmentStatus.Expired)
                return ApiResponse.SuccessResponse();

            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Delivery is not assigning");
        }

        if (delivery.CurrentOfferCourierId != request.CourierId)
        {
            if (lastAttempt?.Status is DeliveryAssignmentStatus.Declined or DeliveryAssignmentStatus.Expired)
                return ApiResponse.SuccessResponse();

            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "No active offer for courier");
        }

        delivery.DeclineOffer(request.CourierId, request.Reason);
        var offered = await _assignmentService.OfferNextCourierAsync(delivery, ct);
        if (!offered)
            _logger.LogWarning("No available couriers after decline for delivery {DeliveryId}", delivery.Id);

        var enqueueAt = offered
            ? (delivery.CurrentOfferExpiresAt ?? DateTime.UtcNow)
            : DateTime.UtcNow.AddMinutes(1);
        await _queue.EnqueueAsync(delivery.Id, enqueueAt, false, ct);
        
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
