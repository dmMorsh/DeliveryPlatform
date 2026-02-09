using MediatR;
using PaymentService.Application.Models;
using Shared.Utilities;

namespace PaymentService.Application.Commands.StartPayment;

public record StartPaymentCommand(Guid OrderId, string Provider, bool Capture) : IRequest<ApiResponse<StartPaymentResult>>;
