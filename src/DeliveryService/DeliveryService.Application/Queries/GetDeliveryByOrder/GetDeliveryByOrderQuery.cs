using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetDeliveryByOrder;

public record GetDeliveryByOrderQuery(Guid OrderId) : IRequest<ApiResponse<DeliveryView>>;
