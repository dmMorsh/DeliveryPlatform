using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using Mapster;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public class ReserveStockCommandHandler
    : IRequestHandler<ReserveStockCommand, ApiResponse<StockView>>
{
    private readonly IStockItemRepository _repository;
    private readonly IUnitOfWork _uow;

    public ReserveStockCommandHandler(
        IStockItemRepository repository,
        IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    public async Task<ApiResponse<StockView>> Handle(
        ReserveStockCommand request,
        CancellationToken ct)
    {
        var stock = await _repository
                        .GetByProductIdAsync(request.ProductId, ct)
                    ?? throw new NotFoundException("Stock item not found");

        stock.Reserve(request.Quantity, request.OrderId);
//TODO fix event. add payload
        var outboxMessages = stock.DomainEvents
            .Select(de => new OutboxMessage
            {
                Id = new Guid(),
                AggregateId = stock.Id,
                Type =  de.GetType().Name,
                    
            })
            .ToList();
        
        await _uow.SaveChangesAsync(outboxMessages, ct);
        stock.ClearDomainEvents();
        
        return ApiResponse<StockView>.SuccessResponse(stock.Adapt<StockView>(), "item reserved");
    }
    
}

public class NotFoundException(string message) : Exception(message);
