using DeliveryService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace DeliveryService.Application.Queries.GetDeliveryByOrder;

public record GetDeliveryByOrderQuery(Guid OrderId) : IRequest<ApiResponse<DeliveryView>>;
