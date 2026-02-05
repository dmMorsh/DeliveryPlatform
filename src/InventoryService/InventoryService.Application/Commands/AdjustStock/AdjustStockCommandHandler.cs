using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AdjustStock;

public class AdjustStockCommandHandler: IRequestHandler<AdjustStockCommand, ApiResponse<List<ProcessedStockItemModel>?>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IShardResolver _resolver;

    public AdjustStockCommandHandler(IUnitOfWorkFactory factory, IShardResolver resolver)
    {
        _factory = factory;
        _resolver = resolver;
    }

    public async Task<ApiResponse<List<ProcessedStockItemModel>?>> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        if (request.Models.Length == 0)
            return ApiResponse<List<ProcessedStockItemModel>?>.ErrorResponse("No item in request");
        var errors = new List<ProcessedStockItemModel>();

        var shardGroups = request.Models
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            _ = await ProcessMessage(errors, shardId, shardGroup.ToArray(), ct);
        }
        
        if (errors.Any())
            return ApiResponse<List<ProcessedStockItemModel>?>.ErrorResponse(errors,"Adjusting failed partly");
        return ApiResponse<List<ProcessedStockItemModel>?>.SuccessResponse(null,"Stock adjusted");
    }

    private async Task<bool> ProcessMessage(List<ProcessedStockItemModel> errors, int shardId,
        SimpleStockItemModel[] baseStockModels, CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId);
        
        foreach (var model in baseStockModels)
        {
            var stock = await uow.Stock
                .GetByProductIdAsync(model.ProductId, ct);
            if (stock == null)
            {
                errors.Add(new ProcessedStockItemModel(
                    model.ProductId,
                    model.Quantity,
                    $"Stock with id {model.ProductId} not found"));
                continue;
            }
            if (model.Quantity - stock.ReservedQuantity >= 0)
                stock.SetTotalQuantity(model.Quantity);
            else
                errors.Add(new ProcessedStockItemModel(
                    model.ProductId,
                    model.Quantity,
                    $"Stock reserved with {stock.ReservedQuantity}"));
        }
        
        await uow.SaveChangesWithoutMessagesAsync(ct);
        return true;
    }
}