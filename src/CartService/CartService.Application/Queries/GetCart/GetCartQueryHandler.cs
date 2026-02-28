using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Application.Services;
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
        if (!CartReadCache.TryGet(request.CustomerId, out var result))
        {
            var task = SingleFlight.RunAsync(
                request.CustomerId,
                token => CartReadCache.LoadAsync(
                    () => _readRepo.GetCartByCustomerIdAsync(request.CustomerId, token)));
            result = await task.WaitAsync(ct);
            CartReadCache.Set(request.CustomerId, result);
        }

        if (result == null)
            return ApiResponse<CartView>.ErrorResponse("Cart not found");
        
        return ApiResponse<CartView>.SuccessResponse(result);
    }
}
