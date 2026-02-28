using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.MarkPaymentReady;

public record MarkPaymentReadyCommand(Guid OrderId) : IRequest<ApiResponse>;
