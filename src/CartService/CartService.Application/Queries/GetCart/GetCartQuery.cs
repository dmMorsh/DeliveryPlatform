using CartService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CartService.Application.Queries.GetCart;

public record GetCartQuery(Guid CustomerId) : IRequest<ApiResponse<CartView>>;