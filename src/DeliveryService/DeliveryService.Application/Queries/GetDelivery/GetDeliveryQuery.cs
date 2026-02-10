using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetDelivery;

public record GetDeliveryQuery(Guid DeliveryId) : IRequest<ApiResponse<DeliveryView>>;
