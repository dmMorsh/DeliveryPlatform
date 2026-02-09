using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.RefundPayment;

public record RefundPaymentCommand(Guid OrderId, long AmountCents) : IRequest<ApiResponse>;
