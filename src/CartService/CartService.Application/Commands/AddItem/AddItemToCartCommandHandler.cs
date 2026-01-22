using MediatR;
using CartService.Application.Interfaces;
using CartService.Domain.Aggregates;
using CartService.Domain.Entities;
using Shared.Utilities;

namespace CartService.Application.Commands.AddItem;

public class AddItemToCartCommandHandler : IRequestHandler<AddItemToCartCommand, ApiResponse<string>>
{
    private readonly ICartRepository _repo;
    private readonly IUnitOfWork _uow;

    public AddItemToCartCommandHandler(ICartRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<string>> Handle(AddItemToCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct) 
                   ?? new Cart(request.CustomerId);

        var item = new CartItem(request.ProductId, request.Name, request.Price, request.Quantity);
        cart.AddItem(item);

        await _repo.CreateOrUpdateAsync(cart, ct);
        
        var outboxMessages = cart.DomainEvents
            .Select(de => new Models.OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = cart.Id,
                Type = de.GetType().Name,
                OccurredAt = DateTime.UtcNow
            })
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();

        return ApiResponse<string>.SuccessResponse(cart.Id.ToString(), "Item added to cart");
    }
}
