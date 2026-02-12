using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.RemoveItem;

public record RemoveItemFromCartCommand(Guid CustomerId, Guid ProductId) : IRequest<ApiResponse>;