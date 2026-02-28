using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderReady;

public record MarkOrderReadyCommand(Guid OrderId) : IRequest<ApiResponse>;
