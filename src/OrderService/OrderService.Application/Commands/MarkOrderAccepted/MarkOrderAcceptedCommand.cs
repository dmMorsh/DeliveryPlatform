using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderAccepted;

public record MarkOrderAcceptedCommand(Guid OrderId) : IRequest<ApiResponse>;
