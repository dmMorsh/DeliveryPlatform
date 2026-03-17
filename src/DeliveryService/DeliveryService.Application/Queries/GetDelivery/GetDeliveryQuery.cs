using DeliveryService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace DeliveryService.Application.Queries.GetDelivery;

public record GetDeliveryQuery(Guid DeliveryId) : IRequest<ApiResponse<DeliveryView>>;
