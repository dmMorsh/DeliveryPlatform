using CartService.Application.Interfaces;
using CartService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CartService.Application.Queries.GetCart;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, ApiResponse<CartView>>
{
    private readonly ICartReadRepository _readRepo;

    public GetCartQueryHandler(ICartReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<CartView>> Handle(GetCartQuery request, CancellationToken ct)
    {
        var result = await _readRepo.GetCartByCustomerIdAsync(request.CustomerId, ct);

        if (result == null)
            return ApiResponse<CartView>.ErrorResponse("Cart not found");
        
        return ApiResponse<CartView>.SuccessResponse(result);
    }
}