using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessSberbankWebhook;

public record ProcessSberbankWebhookCommand(string OrderId, int OrderStatus) : IRequest<ApiResponse>;
