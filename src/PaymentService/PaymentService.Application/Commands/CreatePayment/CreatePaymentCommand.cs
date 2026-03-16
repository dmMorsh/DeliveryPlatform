using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CreatePayment;

public record CreatePaymentCommand(Guid OrderId, long Amount, string Currency) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, Amount, Currency ?? string.Empty);
}