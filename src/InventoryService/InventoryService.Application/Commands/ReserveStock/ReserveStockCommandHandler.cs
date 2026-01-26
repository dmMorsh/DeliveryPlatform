using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Events;
using Mapster;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public class ReserveStockCommandHandler
    : IRequestHandler<ReserveStockCommand, ApiResponse<StockView>>
{
    private readonly IStockItemRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IStockIntegrationEventMapper _eventMapper;

    public ReserveStockCommandHandler(
        IStockItemRepository repository,
        IUnitOfWork uow,
        IStockIntegrationEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<StockView>> Handle(
        ReserveStockCommand request,
        CancellationToken ct)
    {
        var stock = await _repository
                        .GetByProductIdAsync(request.ProductId, ct)
                    ?? throw new NotFoundException("Stock item not found");

        stock.Reserve(request.Quantity, request.OrderId);

        var outboxMessages = new List<OutboxMessage>();

        foreach (var domainEvent in stock.DomainEvents)
        {
            if (domainEvent is StockReservedDomainEvent stockReservedEvent)
            {
                var integrationEvent = _eventMapper.MapStockReservedEvent(
                    stockReservedEvent.ProductId,
                    stockReservedEvent.OrderId,
                    stockReservedEvent.Quantity);
                outboxMessages.Add(OutboxMessage.From(integrationEvent));
            }
        }
        
        await _uow.SaveChangesAsync(outboxMessages, ct);
        stock.ClearDomainEvents();
        
        return ApiResponse<StockView>.SuccessResponse(stock.Adapt<StockView>(), "item reserved");
    }
    
}

public class NotFoundException(string message) : Exception(message);
