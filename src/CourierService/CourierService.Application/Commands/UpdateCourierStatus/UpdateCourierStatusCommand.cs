using Shared.Contracts;
using Shared.Utilities;
using MediatR;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public record UpdateCourierStatusCommand(Guid CourierId, UpdateCourierDto Dto) : IRequest<ApiResponse<CourierDto>>;
