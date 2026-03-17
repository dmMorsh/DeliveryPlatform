using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace DeliveryService.Application.Commands.AcceptDelivery;

public class AcceptDeliveryCommandHandler : IRequestHandler<AcceptDeliveryCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly IDeliveryOfferCache _offerCache;
    private readonly IDeliveryEtaCalculator _etaCalculator;
    private readonly ILogger<AcceptDeliveryCommandHandler> _logger;

    public AcceptDeliveryCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        IDeliveryOfferCache offerCache,
        IDeliveryEtaCalculator etaCalculator,
        ILogger<AcceptDeliveryCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _offerCache = offerCache;
        _etaCalculator = etaCalculator;
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

        // recompute ETA based on potentially updated conditions (e.g. delivery age or courier location)
        var eta = _etaCalculator.Calculate(
            delivery.FromLatitude,
            delivery.FromLongitude,
            delivery.ToLatitude,
            delivery.ToLongitude);
        if (eta != null)
        {
            delivery.SetEta(
                eta.EstimatedPickupAt,
                eta.EstimatedDeliveryAt,
                eta.DistanceKm,
                eta.TravelMinutes);
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
        await _offerCache.RemoveAsync(request.CourierId, ct);

        _logger.LogInformation("Delivery {DeliveryId} accepted by courier {CourierId}", delivery.Id, request.CourierId);
        return ApiResponse.SuccessResponse();
    }
}
