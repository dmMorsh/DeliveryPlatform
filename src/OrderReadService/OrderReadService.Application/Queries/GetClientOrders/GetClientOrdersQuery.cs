using MediatR;
using OrderReadService.Application.Models;
using Shared.Utilities;

namespace OrderReadService.Application.Queries.GetClientOrders;

public record GetClientOrdersQuery(Guid ClientId) : IRequest<ApiResponse<IEnumerable<OrderView>>>;
