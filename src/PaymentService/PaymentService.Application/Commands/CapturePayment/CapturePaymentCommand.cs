using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CapturePayment;

public record CapturePaymentCommand(Guid OrderId, long? AmountCents) : IRequest<ApiResponse>;
