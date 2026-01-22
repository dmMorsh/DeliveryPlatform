using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.AddItem;

public record AddItemToCartCommand(
    Guid CustomerId,
    Guid ProductId,
    string Name,
    int Price,
    int Quantity
) : IRequest<ApiResponse<string>>;
