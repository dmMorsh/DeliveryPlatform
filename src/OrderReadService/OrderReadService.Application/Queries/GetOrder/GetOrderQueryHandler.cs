using MediatR;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.Models;
using OrderReadService.Application.Services;
using Shared.Services;
using Shared.Utilities;

namespace OrderReadService.Application.Queries.GetOrder;

public class GetOrderQueryHandler(IOrderReadRepository repository) : IRequestHandler<GetOrderQuery, ApiResponse<OrderView?>>
{
    private static readonly SingleFlight<Guid, OrderView?> SingleFlight = new();

    public async Task<ApiResponse<OrderView?>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (!OrderReadCache.TryGet(request.OrderId, out var orderView))
        {
            var task = SingleFlight.RunAsync(
                request.OrderId,
                token => OrderReadCache.LoadAsync(
                    () => repository.GetByIdAsync(request.OrderId, token)));
            orderView = await task.WaitAsync(cancellationToken);
            OrderReadCache.Set(request.OrderId, orderView);
        }
        
        if (orderView is null)
            return ApiResponse<OrderView?>.ErrorResponse("Could not find order");
        
        return ApiResponse<OrderView>.SuccessResponse(orderView)!;
    }
}
