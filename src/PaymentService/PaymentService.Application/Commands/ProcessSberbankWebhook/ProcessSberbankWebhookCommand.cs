using MediatR;
using Shared.Contracts;

namespace PaymentService.Application.Commands.ProcessSberbankWebhook;

public record ProcessSberbankWebhookCommand(string OrderId, int OrderStatus) : IRequest<ApiResponse>;
