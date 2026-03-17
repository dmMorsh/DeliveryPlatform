using CartService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CartService.Application.Queries.GetCart;

public record GetCartQuery(Guid CustomerId) : IRequest<ApiResponse<CartView>>;