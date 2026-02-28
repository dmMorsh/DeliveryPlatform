using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, ApiResponse<List<ProcessedStockItemModel>?>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IShardResolver _resolver;

    public AddStockCommandHandler(IUnitOfWorkFactory factory, IShardResolver resolver)
    {
        _factory = factory;
        _resolver = resolver;
    }

    public async Task<ApiResponse<List<ProcessedStockItemModel>?>> Handle(AddStockCommand request, CancellationToken ct)
    {
        if (request.Models.Length == 0)
            return ApiResponse<List<ProcessedStockItemModel>?>.ErrorResponse(ErrorCodes.Validation, "No item in request");
        var errors = new List<ProcessedStockItemModel>();
        
        var shardGroups = request.Models.DistinctBy(x=>x.ProductId)
            .GroupBy(i => _resolver.ResolveShard(i.ProductId));
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            _ = await ProcessMessage(errors, shardId, shardGroup.ToArray(), ct);
        }
        
        if (errors.Any())
            return ApiResponse<List<ProcessedStockItemModel>?>.ErrorResponse(ErrorCodes.Invariant, errors, "Some items were skipped");
        return ApiResponse<List<ProcessedStockItemModel>?>.SuccessResponse(null, "Stocks added");
    }

    private async Task<bool> ProcessMessage(List<ProcessedStockItemModel> errors, int shardId,
        SimpleStockItemModel[] baseStockModels, CancellationToken ct)
    {
        await using var uow = _factory.Create(shardId);
        
        var existStocks = await uow.Stock
            .GetByProductIdsAsync(baseStockModels.Select(m => m.ProductId).ToList(), ct);

        if (existStocks.Any())
        {
            var existingIds = existStocks.Select(s => s.Id).ToHashSet();
            errors.AddRange(baseStockModels
                .Where(m => existingIds.Contains(m.ProductId))
                .Select(i=> new ProcessedStockItemModel(
                    i.ProductId,
                    i.Quantity,
                    "Stock already exist"))
            );
            
            // TODO можно возвращать пропущенные в ответе
            baseStockModels = baseStockModels
                .Where(m => !existingIds.Contains(m.ProductId)).ToArray();
        }
        
        var stocks = baseStockModels.Select(m => 
            new StockItem(
                m.ProductId,
                m.Quantity)
        ).ToArray();
        
        await uow.Stock.AddRangeAsync(stocks, ct);
        try
        {
            await uow.SaveChangesWithoutMessagesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var existing = await uow.Stock
                .GetByProductIdsAsync(baseStockModels.Select(m => m.ProductId).ToList(), ct);
            if (existing.Any())
            {
                var existingIds = existing.Select(s => s.Id).ToHashSet();
                errors.AddRange(baseStockModels
                    .Where(m => existingIds.Contains(m.ProductId))
                    .Select(i => new ProcessedStockItemModel(
                        i.ProductId,
                        i.Quantity,
                        "Stock already exist"))
                );
                return true;
            }
            throw;
        }
        return true;
    }
}