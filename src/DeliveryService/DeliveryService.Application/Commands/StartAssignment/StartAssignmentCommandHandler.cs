using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Models;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.StartAssignment;

public class StartAssignmentCommandHandler : IRequestHandler<StartAssignmentCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IAssignmentService _assignmentService;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<StartAssignmentCommandHandler> _logger;

    public StartAssignmentCommandHandler(
        IDeliveryRepository repository,
        IAssignmentService assignmentService,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<StartAssignmentCommandHandler> logger)
    {
        _repository = repository;
        _assignmentService = assignmentService;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(StartAssignmentCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse("Delivery not found");

        if (delivery.Status == DeliveryStatus.Assigned)
            return ApiResponse.SuccessResponse();

        delivery.StartAssignment();

        var offered = await _assignmentService.OfferNextCourierAsync(delivery, ct);
        if (!offered)
            _logger.LogWarning("No available couriers for delivery {DeliveryId}", delivery.Id);

        await _repository.UpdateAsync(delivery, ct);

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
