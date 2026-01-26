using MediatR;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Aggregates;
using CartService.Domain.Entities;
using Shared.Utilities;

namespace CartService.Application.Commands.AddItem;

public class AddItemToCartCommandHandler : IRequestHandler<AddItemToCartCommand, ApiResponse<string>>
{
    private readonly ICartRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICartIntegrationEventMapper _eventMapper;

    public AddItemToCartCommandHandler(ICartRepository repo, IUnitOfWork uow, ICartIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<string>> Handle(AddItemToCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct) 
                   ?? new Cart(request.CustomerId);

        var model = request.Model;
        var item = new CartItem(model.ProductId, model.Name, model.Price, model.Quantity);
        cart.AddItem(item);

        await _repo.CreateOrUpdateAsync(cart, ct);
        
        var outboxMessages = new List<Models.OutboxMessage>();
        
        foreach (var domainEvent in cart.DomainEvents)
        {
            var integrationEvent = _eventMapper.MapFromDomainEvent(domainEvent);
            if (integrationEvent != null)
            {
                outboxMessages.Add(Models.OutboxMessage.From(integrationEvent));
            }
        }

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();

        return ApiResponse<string>.SuccessResponse(cart.Id.ToString(), "Item added to cart");
    }
}
