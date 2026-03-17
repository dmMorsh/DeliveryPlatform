using CourierService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CourierService.Application.Queries.GetActiveCouriers;

public record GetActiveCouriersQuery : IRequest<ApiResponse<List<CourierView>>>;
