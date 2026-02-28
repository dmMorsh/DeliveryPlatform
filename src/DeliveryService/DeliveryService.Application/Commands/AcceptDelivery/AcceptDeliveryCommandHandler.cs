using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.AcceptDelivery;

public class AcceptDeliveryCommandHandler : IRequestHandler<AcceptDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<AcceptDeliveryCommandHandler> _logger;

    public AcceptDeliveryCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<AcceptDeliveryCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(AcceptDeliveryCommand request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        if (delivery.Status == DeliveryStatus.Assigned)
        {
            if (delivery.CourierId == request.CourierId)
                return ApiResponse.SuccessResponse();

            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Courier mismatch");
        }

        if (delivery.Status != DeliveryStatus.Assigning)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Delivery is not assigning");

        if (delivery.CurrentOfferCourierId != request.CourierId)
        {
            var alreadyProcessed = delivery.AssignmentAttempts.Any(a =>
                a.CourierId == request.CourierId &&
                a.Status != DeliveryAssignmentStatus.Offered);
            if (alreadyProcessed)
                return ApiResponse.SuccessResponse();

            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "No active offer for courier");
        }

        delivery.AcceptOffer(request.CourierId);

        var outbox = delivery.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(e => e != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outbox, ct);
        delivery.ClearDomainEvents();
        DeliveryReadCache.Invalidate(delivery.Id, delivery.OrderId);

        _logger.LogInformation("Delivery {DeliveryId} accepted by courier {CourierId}", delivery.Id, request.CourierId);
        return ApiResponse.SuccessResponse();
    }
}
