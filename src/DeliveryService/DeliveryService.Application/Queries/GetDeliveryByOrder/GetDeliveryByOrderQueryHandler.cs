using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Application.Services;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;
using Shared.Services;

namespace DeliveryService.Application.Queries.GetDeliveryByOrder;

public class GetDeliveryByOrderQueryHandler : IRequestHandler<GetDeliveryByOrderQuery, ApiResponse<DeliveryView>>
{
    private static readonly SingleFlight<Guid, Delivery?> SingleFlight = new();
    private readonly IDeliveryRepository _repository;

    public GetDeliveryByOrderQueryHandler(IDeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<DeliveryView>> Handle(GetDeliveryByOrderQuery request, CancellationToken ct)
    {
        if (!DeliveryReadCache.TryGetByOrderId(request.OrderId, out var delivery))
        {
            var task = SingleFlight.RunAsync(
                request.OrderId,
                token => DeliveryReadCache.LoadAsync(
                    () => _repository.GetByOrderIdAsync(request.OrderId, token)));
            delivery = await task.WaitAsync(ct);
            DeliveryReadCache.SetByOrderId(request.OrderId, delivery);
            if (delivery != null)
                DeliveryReadCache.SetByDeliveryId(delivery.Id, delivery);
        }
        if (delivery == null)
            return ApiResponse<DeliveryView>.ErrorResponse(ErrorCodes.NotFound, "Delivery not found");

        return ApiResponse<DeliveryView>.SuccessResponse(DeliveryView.From(delivery));
    }
}
