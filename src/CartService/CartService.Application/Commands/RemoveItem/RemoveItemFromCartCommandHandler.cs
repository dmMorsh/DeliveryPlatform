using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Application.Services;
using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.RemoveItem;

public class RemoveItemFromCartCommandHandler: IRequestHandler<RemoveItemFromCartCommand, ApiResponse>
{
    private readonly ICartRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICartIntegrationEventMapper _eventMapper;

    public RemoveItemFromCartCommandHandler(ICartRepository repo, IUnitOfWork uow, ICartIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(RemoveItemFromCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);
        if (cart == null)
            return ApiResponse.SuccessResponse("Item removed from cart");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null)
            return ApiResponse.SuccessResponse("Item removed from cart");
        
        cart.RemoveItem(item);
        
        var outboxMessages = cart.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();
        CartReadCache.Invalidate(request.CustomerId);

        return ApiResponse.SuccessResponse("Item removed from cart");
    }
}
