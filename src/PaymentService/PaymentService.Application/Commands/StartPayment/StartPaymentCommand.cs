using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.StartPayment;

public record StartPaymentCommand(Guid OrderId, string Provider) : IRequest<ApiResponse>;