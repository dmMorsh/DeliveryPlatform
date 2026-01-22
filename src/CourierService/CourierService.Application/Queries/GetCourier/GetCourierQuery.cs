using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetCourier;

public record GetCourierQuery(Guid CourierId) : IRequest<ApiResponse<CourierDto>>;
