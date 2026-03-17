using MediatR;
using Shared.Contracts;

namespace PaymentService.Application.Commands.CapturePayment;

public record CapturePaymentCommand(Guid OrderId, long? AmountCents) : IRequest<ApiResponse>;
