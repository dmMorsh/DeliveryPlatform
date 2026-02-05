using Grpc.Core;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Models;
using Shared.Proto;

namespace OrderService.Api.Grpc;

[Authorize(Roles = "Customer")]
public class OrderGrpcService: OrderGrpc.OrderGrpcBase
{
    private readonly IMediator _mediator;

    public OrderGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var createOrderModel = request.Adapt<CreateOrderModel>();
        var cmd = new CreateOrderCommand(createOrderModel);

        var res = await _mediator.Send(cmd);

        if (!res.Success)
        {
            var msg = res.Message ?? (res.Errors != null && res.Errors.Count > 0 ? string.Join(';', res.Errors) : "Internal error");
            throw new RpcException(new Status(StatusCode.Internal, msg));
        }

        return new CreateOrderResponse { OrderId = res.Data!.Id.ToString() };
    }
}
