using MediatR;
using OrderReadService.Application.Models;
using Shared.Utilities;

namespace OrderReadService.Application.Queries.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<ApiResponse<OrderView?>>;