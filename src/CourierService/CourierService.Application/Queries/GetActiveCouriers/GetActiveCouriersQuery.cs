using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetActiveCouriers;

public record GetActiveCouriersQuery : IRequest<ApiResponse<List<CourierView>>>;
