using CartService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.AddItem;

public record AddItemToCartCommand(Guid CustomerId, AddItemModel Model) : IRequest<ApiResponse<string>>;
