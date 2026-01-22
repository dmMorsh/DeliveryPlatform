using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetActiveCouriers;

public record GetActiveCouriersQuery : IRequest<ApiResponse<List<CourierDto>>>;
