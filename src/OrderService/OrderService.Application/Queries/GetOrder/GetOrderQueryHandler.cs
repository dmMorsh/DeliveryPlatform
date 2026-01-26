using MediatR;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Queries.GetOrder;

public class GetOrderQueryHandler(IOrderReadRepository repository) : IRequestHandler<GetOrderQuery, ApiResponse<OrderView?>>
{
    public async Task<ApiResponse<OrderView?>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var orderView = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        
        if (orderView is null)
            return ApiResponse<OrderView?>.ErrorResponse("Could not find order");
        
        return ApiResponse<OrderView>.SuccessResponse(orderView)!;
    }
}