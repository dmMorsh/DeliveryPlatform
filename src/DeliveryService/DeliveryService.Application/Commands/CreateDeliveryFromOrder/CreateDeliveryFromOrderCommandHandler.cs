using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace DeliveryService.Application.Commands.CreateDeliveryFromOrder;

public class CreateDeliveryFromOrderCommandHandler : IRequestHandler<CreateDeliveryFromOrderCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<CreateDeliveryFromOrderCommandHandler> _logger;
    private readonly IDeliveryEtaCalculator _etaCalculator;

    public CreateDeliveryFromOrderCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<CreateDeliveryFromOrderCommandHandler> logger,
        IDeliveryEtaCalculator etaCalculator)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
        _etaCalculator = etaCalculator;
    }

    public async Task<ApiResponse> Handle(CreateDeliveryFromOrderCommand request, CancellationToken ct)
    {
        var existing = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (existing != null)
            return ApiResponse.SuccessResponse();

        var delivery = DeliveryFactory.CreateFromOrder(request);
        delivery.SetDeliveryZone(
            request.DeliveryZoneId,
            request.DeliveryZoneName,
            request.DeliveryPickupSlaMinutes,
            request.DeliveryTransitSlaMinutes,
            request.DeliveryFeeMultiplier,
            request.DeliveryZoneDistanceKm);
        var eta = _etaCalculator.Calculate(
            request.FromLatitude,
            request.FromLongitude,
            request.ToLatitude,
            request.ToLongitude);
        if (eta != null)
        {
            delivery.SetEta(
                eta.EstimatedPickupAt,
                eta.EstimatedDeliveryAt,
                eta.DistanceKm,
                eta.TravelMinutes);
        }

        await _repository.AddAsync(delivery, ct);

        var outbox = delivery.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(e => e != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outbox, ct);
        delivery.ClearDomainEvents();
        DeliveryReadCache.Invalidate(delivery.Id, delivery.OrderId);

        _logger.LogInformation("Delivery created for order {OrderId} => {DeliveryId}", request.OrderId, delivery.Id);
        return ApiResponse.SuccessResponse();
    }
}
