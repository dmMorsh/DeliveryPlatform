using MediatR;
using CartService.Application.Interfaces;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, ApiResponse<string>>
{
    private readonly ICartRepository _repo;
    private readonly IUnitOfWork _uow;

    public CheckoutCartCommandHandler(ICartRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<string>> Handle(CheckoutCartCommand request, CancellationToken ct)
    {
        var cart = await _repo.GetCartByCustomerIdAsync(request.CustomerId, ct);

        if (cart == null || !cart.Items.Any())
            return ApiResponse<string>.ErrorResponse("Cart is empty or not found");

        cart.Checkout();
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

        return ApiResponse<string>.SuccessResponse(cart.Id.ToString(), "Cart checked out successfully");
    }
}
