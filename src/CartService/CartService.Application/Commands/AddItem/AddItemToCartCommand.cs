using MediatR;
using Shared.Contracts;

namespace CartService.Application.Commands.AddItem;

public record AddItemToCartCommand(
    Guid CustomerId,
    Guid ProductId,
    string Name,
    int PriceCents,
    int Quantity) : IRequest<ApiResponse<Guid>>;
