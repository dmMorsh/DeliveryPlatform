using MediatR;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.Models;
using Shared.Contracts;

namespace OrderReadService.Application.Queries.GetClientOrders;

public class GetClientOrdersQueryHandler
    : IRequestHandler<GetClientOrdersQuery, ApiResponse<IEnumerable<OrderView>>>
{
    private readonly IOrderReadRepository _readRepo;

    public GetClientOrdersQueryHandler(IOrderReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<IEnumerable<OrderView>>> Handle(
        GetClientOrdersQuery request,
        CancellationToken ct)
    {
        var res = await _readRepo.GetByClientIdAsync(request.ClientId, ct);
        if (res.Any())
            return ApiResponse<IEnumerable<OrderView>>.SuccessResponse(res, "Orders provided successfully");
        
        return ApiResponse<IEnumerable<OrderView>>.ErrorResponse("No orders found");
    }
}
