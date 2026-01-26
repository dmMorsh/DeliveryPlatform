using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Queries.GetClientOrders;

public record GetClientOrdersQuery(Guid ClientId) : IRequest<ApiResponse<IEnumerable<OrderView>>>;
