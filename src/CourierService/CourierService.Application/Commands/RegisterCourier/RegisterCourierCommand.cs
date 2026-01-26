using CourierService.Application.Models;
using Shared.Utilities;
using MediatR;

namespace CourierService.Application.Commands.RegisterCourier;

public record RegisterCourierCommand(CreateCourierModel Model) : IRequest<ApiResponse<CourierView>>;
