using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetDelivery;

public class GetDeliveryQueryHandler : IRequestHandler<GetDeliveryQuery, ApiResponse<DeliveryView>>
{
    private readonly IDeliveryRepository _repository;

    public GetDeliveryQueryHandler(IDeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<DeliveryView>> Handle(GetDeliveryQuery request, CancellationToken ct)
    {
        var delivery = await _repository.GetByIdAsync(request.DeliveryId, ct);
        if (delivery == null)
            return ApiResponse<DeliveryView>.ErrorResponse("Delivery not found");

        return ApiResponse<DeliveryView>.SuccessResponse(DeliveryView.From(delivery));
    }
}
