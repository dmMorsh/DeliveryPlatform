using MediatR;

namespace OrderService.Application.Queries.GetClientOrders;

public record GetClientOrdersQuery(Guid ClientId) : IRequest<List<OrderView>>;
