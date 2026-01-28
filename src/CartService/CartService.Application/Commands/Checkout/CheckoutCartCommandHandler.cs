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
    private readonly IOrderService _orderService;

    public CheckoutCartCommandHandler(ICartRepository repo, IUnitOfWork uow, ICartIntegrationEventMapper eventMapper, IOrderService orderService)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
        _orderService = orderService;
    }

    public async Task<ApiResponse<string>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);

        if (cart == null || cart.Items.Count == 0)
            return ApiResponse<string>.ErrorResponse("Cart is empty or not found");

        var orderId = await _orderService.CreateOrderFromCartAsync(cart, request.Model, ct);
        
        cart.Checkout(orderId);
        
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

        return ApiResponse<string>.SuccessResponse(orderId.ToString(), "Cart checked out successfully");
    }
}
