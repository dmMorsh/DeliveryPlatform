using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace CartService.Application.Commands.RemoveItem;

public record RemoveItemFromCartCommand(Guid CustomerId, Guid ProductId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(CustomerId, ProductId);
}