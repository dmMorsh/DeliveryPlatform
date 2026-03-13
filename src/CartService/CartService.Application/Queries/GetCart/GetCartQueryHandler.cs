using CartService.Application.Interfaces;
using CartService.Application.Models;
using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace CartService.Application.Queries.GetCart;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, ApiResponse<CartView>>
{
    private static readonly SingleFlight<Guid, CartView?> SingleFlight = new();
    private readonly ICartReadRepository _readRepo;

    public GetCartQueryHandler(ICartReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<CartView>> Handle(GetCartQuery request, CancellationToken ct)
    {
        var task = SingleFlight.RunAsync(
            request.CustomerId,
            token => _readRepo.GetCartByCustomerIdAsync(request.CustomerId, token));
        var result = await task.WaitAsync(ct);

        if (result == null)
            return ApiResponse<CartView>.ErrorResponse("Cart not found");
        
        return ApiResponse<CartView>.SuccessResponse(result);
    }
}
