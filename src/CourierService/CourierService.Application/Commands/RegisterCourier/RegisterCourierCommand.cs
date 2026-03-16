using CourierService.Application.Models;
using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace CourierService.Application.Commands.RegisterCourier;

public record RegisterCourierCommand(
    string FullName,
    string Phone,
    string Email,
    string DocumentNumber
) : IRequest<ApiResponse<CourierView>>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        (Phone ?? string.Empty).Trim().ToLowerInvariant());
}