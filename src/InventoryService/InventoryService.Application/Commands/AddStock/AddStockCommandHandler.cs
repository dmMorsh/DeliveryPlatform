using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AddStock;

public class AddStockCommandHandler : IRequestHandler<AddStockCommand, ApiResponse<List<ProcessedStockItemModel>?>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IShardResolver _resolver;
    private readonly IStockIntegrationEventMapper _eventMapper;

    public AddStockCommandHandler(IUnitOfWorkFactory factory, IShardResolver resolver, IStockIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _resolver = resolver;
        _eventMapper = eventMapper;
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

        var invalidItems = baseStockModels
            .Where(m => m.Quantity <= 0)
            .Select(m => new ProcessedStockItemModel(
                m.ProductId,
                m.Quantity,
                "Quantity must be positive"))
            .ToList();
        if (invalidItems.Count > 0)
        {
            errors.AddRange(invalidItems);
            baseStockModels = baseStockModels.Where(m => m.Quantity > 0).ToArray();
        }
        if (baseStockModels.Length == 0)
            return true;
        
        var existStocks = await uow.Stock
            .GetByProductIdsAsync(baseStockModels.Select(m => m.ProductId).ToList(), ct);

        var existingById = existStocks.ToDictionary(s => s.Id);
        var updatedStocks = new List<StockItem>();

        foreach (var model in baseStockModels)
        {
            if (existingById.TryGetValue(model.ProductId, out var stock))
            {
                stock.AddStock(model.Quantity);
                updatedStocks.Add(stock);
                continue;
            }

            var newStock = new StockItem(model.ProductId, model.Quantity);
            await uow.Stock.AddAsync(newStock, ct);
            updatedStocks.Add(newStock);
        }

        var outboxMessages = updatedStocks
            .Select(s => _eventMapper.MapStockQuantityChangedEvent(
                s.Id,
                s.TotalQuantity,
                s.ReservedQuantity,
                s.AvailableQuantity))
            .Select(OutboxMessage.From)
            .ToList();

        await uow.SaveChangesAsync(outboxMessages, ct);
        return true;
    }
}
