using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Queries.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<ApiResponse<OrderView?>>;