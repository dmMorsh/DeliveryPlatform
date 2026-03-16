using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, ApiResponse<Guid>>
{
    private readonly ICartRepository _repo;
    private readonly ICartReadCache _readCache;
    private readonly IUnitOfWork _uow;
    private readonly ICartIntegrationEventMapper _eventMapper;
    private readonly IOrderService _orderService;

    public CheckoutCartCommandHandler(
        ICartRepository repo,
        ICartReadCache readCache,
        IUnitOfWork uow,
        ICartIntegrationEventMapper eventMapper,
        IOrderService orderService)
    {
        _repo = repo;
        _readCache = readCache;
        _uow = uow;
        _eventMapper = eventMapper;
        _orderService = orderService;
    }

    public async Task<ApiResponse<Guid>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);

        if (cart == null || cart.Items.Count == 0)
            return ApiResponse<Guid>.ErrorResponse("Cart is empty or not found");

        var orderId = await _orderService.CreateOrderFromCartAsync(cart, request, ct);
        
        cart.Checkout(orderId);
        
        var outboxMessages = cart.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        cart.ClearDomainEvents();
        await _readCache.SetAsync(request.CustomerId, CartViewMapper.ToView(cart), ct);

        return ApiResponse<Guid>.SuccessResponse(orderId, "Cart checked out successfully");
    }
}
