using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetCourierOffer;

public sealed class GetCourierOfferQueryHandler : IRequestHandler<GetCourierOfferQuery, ApiResponse<CourierOfferView?>>
{
    private readonly IDeliveryRepository _repository;

    public GetCourierOfferQueryHandler(IDeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<CourierOfferView?>> Handle(GetCourierOfferQuery request, CancellationToken ct)
    {
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

        return ApiResponse<CourierOfferView?>.SuccessResponse(view);
    }
}
