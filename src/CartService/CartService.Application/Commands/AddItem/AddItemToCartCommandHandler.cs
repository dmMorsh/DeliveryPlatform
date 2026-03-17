using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Aggregates;
using MediatR;
using Shared.Contracts;

namespace CartService.Application.Commands.AddItem;

public class AddItemToCartCommandHandler : IRequestHandler<AddItemToCartCommand, ApiResponse<Guid>>
{
    private readonly ICartRepository _repo;
    private readonly ICartReadCache _readCache;
    private readonly IUnitOfWork _uow;
    private readonly ICartIntegrationEventMapper _eventMapper;

    public AddItemToCartCommandHandler(
        ICartRepository repo,
        ICartReadCache readCache,
        IUnitOfWork uow,
        ICartIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _readCache = readCache;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<Guid>> Handle(AddItemToCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);
        if (cart == null)
        {
            cart = new Cart(request.CustomerId);
            await _repo.AddAsync(cart, ct);
        }

        cart.AddItem(request.ProductId, request.Name, request.PriceCents, request.Quantity);
        
       var outboxMessages = cart.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();
        await _readCache.SetAsync(request.CustomerId, CartViewMapper.ToView(cart), ct);

        return ApiResponse<Guid>.SuccessResponse(cart.Id, "Item added to cart");
    }
}
