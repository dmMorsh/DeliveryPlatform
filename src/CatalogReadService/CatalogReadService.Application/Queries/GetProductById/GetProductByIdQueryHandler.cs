using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.Models;
using CatalogReadService.Application.Services;
using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace CatalogReadService.Application.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductView>>
{
    private static readonly SingleFlight<Guid, ProductView?> SingleFlight = new();
    private readonly IProductReadRepository _readRepo;

    public GetProductByIdQueryHandler(IProductReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<ProductView>> Handle(
        GetProductByIdQuery request,
        CancellationToken ct)
    {
        if (!ProductReadCache.TryGet(request.Id, out var result))
        {
            var task = SingleFlight.RunAsync(
                request.Id,
                token => ProductReadCache.LoadAsync(
                    () => _readRepo.GetByIdAsync(request.Id, token)));
            result = await task.WaitAsync(ct);
            ProductReadCache.Set(request.Id, result);
        }

        if (result == null)
            return ApiResponse<ProductView>.ErrorResponse("Product not found");

        return ApiResponse<ProductView>.SuccessResponse(result);
    }
}
