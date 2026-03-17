using System.Diagnostics.Metrics;
using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace DeliveryService.Application.Commands.StartAssignment;

public class StartAssignmentCommandHandler : IRequestHandler<StartAssignmentCommand, ApiResponse>
{
    private static readonly Meter Meter = new("DeliveryService.Assignment", "1.0.0");
    private static readonly Counter<long> EnqueueTotal = Meter.CreateCounter<long>("delivery_assignment_enqueue_total");

    private readonly IDeliveryRepository _repository;
    private readonly IAssignmentService _assignmentService;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly IAssignmentQueue _queue;
    private readonly ILogger<StartAssignmentCommandHandler> _logger;

    public StartAssignmentCommandHandler(
        IDeliveryRepository repository,
        IAssignmentService assignmentService,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        IAssignmentQueue queue,
        ILogger<StartAssignmentCommandHandler> logger)
    {
        _repository = repository;
        _assignmentService = assignmentService;
        _uow = uow;
        _eventMapper = eventMapper;
        _queue = queue;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(StartAssignmentCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        if (delivery.Status == DeliveryStatus.Assigned)
            return ApiResponse.SuccessResponse();

        delivery.StartAssignment();

        var offered = await _assignmentService.OfferNextCourierAsync(delivery, ct);
        if (!offered)
            _logger.LogWarning("No available couriers for delivery {DeliveryId}", delivery.Id);

        var enqueueAt = delivery.CurrentOfferExpiresAt ?? DateTime.UtcNow;
        await _queue.EnqueueAsync(delivery.Id, enqueueAt, false, ct);
        EnqueueTotal.Add(1);
        
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
