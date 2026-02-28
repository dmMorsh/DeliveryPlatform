using DeliveryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Queries.GetCourierOffer;

public record GetCourierOfferQuery(Guid CourierId) : IRequest<ApiResponse<CourierOfferView?>>;
