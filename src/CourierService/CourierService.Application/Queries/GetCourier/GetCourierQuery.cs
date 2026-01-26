using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetCourier;

public record GetCourierQuery(Guid CourierId) : IRequest<ApiResponse<CourierView>>;
