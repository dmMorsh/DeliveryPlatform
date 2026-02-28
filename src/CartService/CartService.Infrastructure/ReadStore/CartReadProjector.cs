using CartService.Infrastructure.Repositories;
using Shared.Contracts.Events;

namespace CartService.Infrastructure.ReadStore;

public class CartReadProjector
{
    private readonly CartReadRepository _readRepository;

    public CartReadProjector(CartReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task HandleAsync(CartItemAddedEvent evt, CancellationToken ct)
    {
        // Cart changed, invalidate cache for customer
        // We don't know the customer ID from the event, so we'll invalidate on checkout
        // For now, this is handled by the repository cache TTL (1 hour)
        // Alternatively, we could store cartId->customerId mapping in Redis
        await Task.CompletedTask;
    }

    public async Task HandleAsync(CartCheckedOutEvent evt, CancellationToken ct)
    {
        // Invalidate cache for this customer after checkout
        await _readRepository.InvalidateCacheAsync(evt.CustomerId, ct);
    }
}
