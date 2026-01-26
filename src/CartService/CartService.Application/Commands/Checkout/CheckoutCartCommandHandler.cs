using MediatR;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Domain.Events;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, ApiResponse<string>>
{
    private readonly ICartRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICartIntegrationEventMapper _eventMapper;

    public CheckoutCartCommandHandler(ICartRepository repo, IUnitOfWork uow, ICartIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<string>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);

        if (cart == null || cart.Items.Count == 0)
            return ApiResponse<string>.ErrorResponse("Cart is empty or not found");

        cart.Checkout();
        await _repo.CreateOrUpdateAsync(cart, ct);

        var outboxMessages = new List<Models.OutboxMessage>();
        
        foreach (var domainEvent in cart.DomainEvents)
        {
            if (domainEvent is CartDomainEvent cartEvent)
            {
                var integrationEvent = _eventMapper.MapFromDomainEvent(cartEvent);
                if (integrationEvent != null)
                {
                    outboxMessages.Add(Models.OutboxMessage.From(integrationEvent));
                }
            }
        }

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();

        return ApiResponse<string>.SuccessResponse(cart.Id.ToString(), "Cart checked out successfully");
    }
}
