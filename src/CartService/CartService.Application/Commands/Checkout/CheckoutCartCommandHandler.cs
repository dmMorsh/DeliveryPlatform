using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Application.Models;
using MediatR;
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
        
        var outboxMessages = cart.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();

        return ApiResponse<string>.SuccessResponse(orderId.ToString(), "Cart checked out successfully");
    }
}
