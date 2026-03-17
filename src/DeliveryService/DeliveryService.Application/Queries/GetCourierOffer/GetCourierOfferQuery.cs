using DeliveryService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace DeliveryService.Application.Queries.GetCourierOffer;

public record GetCourierOfferQuery(Guid CourierId) : IRequest<ApiResponse<CourierOfferView?>>;
