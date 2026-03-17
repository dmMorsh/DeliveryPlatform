using CourierService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CourierService.Application.Queries.GetCourier;

public record GetCourierQuery(Guid CourierId) : IRequest<ApiResponse<CourierView>>;
