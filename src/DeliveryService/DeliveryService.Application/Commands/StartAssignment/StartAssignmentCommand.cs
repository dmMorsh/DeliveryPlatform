using MediatR;
using Shared.Contracts;

namespace DeliveryService.Application.Commands.StartAssignment;

public record StartAssignmentCommand(Guid OrderId) : IRequest<ApiResponse>;
