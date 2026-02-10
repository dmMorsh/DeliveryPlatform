using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetDeliveryByOrder;

public class GetDeliveryByOrderQueryHandler : IRequestHandler<GetDeliveryByOrderQuery, ApiResponse<DeliveryView>>
{
    private readonly IDeliveryRepository _repository;

    public GetDeliveryByOrderQueryHandler(IDeliveryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<DeliveryView>> Handle(GetDeliveryByOrderQuery request, CancellationToken ct)
    {
        var delivery = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (delivery == null)
            return ApiResponse<DeliveryView>.ErrorResponse("Delivery not found");

        return ApiResponse<DeliveryView>.SuccessResponse(DeliveryView.From(delivery));
    }
}
