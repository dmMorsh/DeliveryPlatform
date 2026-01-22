using Shared.Contracts;
using Shared.Utilities;
using MediatR;

namespace CourierService.Application.Commands.RegisterCourier;

public record RegisterCourierCommand(CreateCourierDto Dto) : IRequest<ApiResponse<CourierDto>>;
