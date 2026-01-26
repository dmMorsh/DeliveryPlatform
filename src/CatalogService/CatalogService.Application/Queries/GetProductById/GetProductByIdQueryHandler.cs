using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductView>>
{
    private readonly IProductReadRepository _readRepo;

    public GetProductByIdQueryHandler(IProductReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<ProductView>> Handle(
        GetProductByIdQuery request, 
        CancellationToken ct)
    {
        var result = await _readRepo.GetByIdAsync(request.Id, ct);

        if (result == null)
            return ApiResponse<ProductView>.ErrorResponse("Product not found");
        
        return ApiResponse<ProductView>.SuccessResponse(result);
    }
}