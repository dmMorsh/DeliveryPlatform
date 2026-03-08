using MediatR;
using OrderReadService.Application.Models;
using Shared.Utilities;

namespace OrderReadService.Application.Queries.GetOrder;

public record GetOrderQuery(Guid OrderId, Guid CustomerId) : IRequest<ApiResponse<OrderView?>>;