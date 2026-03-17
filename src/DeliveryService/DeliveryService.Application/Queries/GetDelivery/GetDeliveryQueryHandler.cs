using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;
using Shared.Services;

namespace DeliveryService.Application.Queries.GetDelivery;

public class GetDeliveryQueryHandler : IRequestHandler<GetDeliveryQuery, ApiResponse<DeliveryView>>
{
    private static readonly SingleFlight<Guid, Delivery?> SingleFlight = new();
    private readonly IDeliveryRepository _repository;

    public GetDeliveryQueryHandler(IDeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<DeliveryView>> Handle(GetDeliveryQuery request, CancellationToken ct)
    {
        if (!DeliveryReadCache.TryGetByDeliveryId(request.DeliveryId, out var delivery))
        {
            var task = SingleFlight.RunAsync(
                request.DeliveryId,
                token => DeliveryReadCache.LoadAsync(
                    () => _repository.GetByIdAsync(request.DeliveryId, token)));
            delivery = await task.WaitAsync(ct);
            DeliveryReadCache.SetByDeliveryId(request.DeliveryId, delivery);
            if (delivery != null)
                DeliveryReadCache.SetByOrderId(delivery.OrderId, delivery);
        }
        if (delivery == null)
            return ApiResponse<DeliveryView>.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        return ApiResponse<DeliveryView>.SuccessResponse(DeliveryView.From(delivery));
    }
}
