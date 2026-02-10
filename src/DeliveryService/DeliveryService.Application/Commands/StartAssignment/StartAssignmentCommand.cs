using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.StartAssignment;

public record StartAssignmentCommand(Guid OrderId) : IRequest<ApiResponse>;
