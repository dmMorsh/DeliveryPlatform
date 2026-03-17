using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace PaymentService.Application.Commands.MarkPaymentReady;

public record MarkPaymentReadyCommand(Guid OrderId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, "ready");
}