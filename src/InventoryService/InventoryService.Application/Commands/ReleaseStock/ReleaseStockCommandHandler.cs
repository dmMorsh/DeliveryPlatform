using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Domain.Events;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReleaseStock;

public class ReleaseStockCommandHandler
    : IRequestHandler<ReleaseStockCommand, ApiResponse<Unit>>
{
    private readonly IStockItemRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IStockIntegrationEventMapper _eventMapper;

    public ReleaseStockCommandHandler(
        IStockItemRepository repository,
        IUnitOfWork uow,
        IStockIntegrationEventMapper eventMapper)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<Unit>> Handle(
        ReleaseStockCommand request,
        CancellationToken ct)
    {
        var stock = await _repository
                        .GetByProductIdAsync(request.ProductId, ct)
                    ?? throw new NotFoundException("Stock item not found");

        stock.Release(request.Quantity, request.OrderId);

        var outboxMessages = new List<OutboxMessage>();

        foreach (var domainEvent in stock.DomainEvents)
        {
            if (domainEvent is StockReleasedDomainEvent stockReleasedEvent)
            {
                var integrationEvent = _eventMapper.MapStockReleasedEvent(
                    stockReleasedEvent.ProductId,
                    stockReleasedEvent.OrderId,
                    stockReleasedEvent.Quantity);
                outboxMessages.Add(OutboxMessage.From(integrationEvent));
            }
        }
        
        await _uow.SaveChangesAsync(outboxMessages, ct);
        stock.ClearDomainEvents();
        
        return ApiResponse<Unit>.SuccessResponse(Unit.Value, "Stock released");
    }
}

public class NotFoundException(string message) : Exception(message);
