using MediatR;
using OrderService.Application.Interfaces;

namespace OrderService.Application.Queries.GetClientOrders;

public class GetClientOrdersQueryHandler
    : IRequestHandler<GetClientOrdersQuery, List<OrderView>>
{
    private readonly IOrderReadRepository _readRepo;

    public GetClientOrdersQueryHandler(IOrderReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<List<OrderView>> Handle(
        GetClientOrdersQuery request,
        CancellationToken ct)
    {
        return await _readRepo.GetByClientIdAsync(request.ClientId, ct);
    }
}
