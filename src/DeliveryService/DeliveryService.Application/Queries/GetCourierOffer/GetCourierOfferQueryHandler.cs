using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace DeliveryService.Application.Queries.GetCourierOffer;

public sealed class GetCourierOfferQueryHandler : IRequestHandler<GetCourierOfferQuery, ApiResponse<CourierOfferView?>>
{
    private readonly IDeliveryRepository _repository;
    private readonly IDeliveryOfferCache _cache;

    public GetCourierOfferQueryHandler(IDeliveryRepository repository, IDeliveryOfferCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ApiResponse<CourierOfferView?>> Handle(GetCourierOfferQuery request, CancellationToken ct)
    {
        // Try cache first
        var cachedOffer = await _cache.GetAsync(request.CourierId, ct);
        if (cachedOffer != null)
            return ApiResponse<CourierOfferView?>.SuccessResponse(cachedOffer);

        var now = DateTime.UtcNow;
        var delivery = await _repository.GetActiveOfferByCourierIdAsync(request.CourierId, now, ct);
        if (delivery == null)
            return ApiResponse<CourierOfferView?>.SuccessResponse(null);

        var view = new CourierOfferView
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId,
            FromAddress = delivery.FromAddress,
            ToAddress = delivery.ToAddress,
            FromLatitude = delivery.FromLatitude,
            FromLongitude = delivery.FromLongitude,
            ToLatitude = delivery.ToLatitude,
            ToLongitude = delivery.ToLongitude,
            ExpiresAt = delivery.CurrentOfferExpiresAt,
            EstimatedPickupAt = delivery.EstimatedPickupAt,
            EstimatedDeliveryAt = delivery.EstimatedDeliveryAt,
            EstimatedDistanceKm = delivery.EstimatedDistanceKm,
            EstimatedTravelMinutes = delivery.EstimatedTravelMinutes
        };

        // Cache the result
        await _cache.SetAsync(request.CourierId, view, ct);

        return ApiResponse<CourierOfferView?>.SuccessResponse(view);
    }
}
