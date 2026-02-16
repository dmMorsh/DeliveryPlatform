using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using PaymentService.Application.Commands.CancelPayment;
using PaymentService.Application.Commands.CapturePayment;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Commands.RefundPayment;
using PaymentService.Application.Commands.StartPayment;
using PaymentService.Application.Interfaces;
using Shared.Proto;
using Shared.Utilities;

namespace PaymentService.Api.Grpc;

[Authorize]
public sealed class PaymentGrpcService : PaymentGrpc.PaymentGrpcBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWorkFactory _factory;

    public PaymentGrpcService(IMediator mediator, IUnitOfWorkFactory factory)
    {
        _mediator = mediator;
        _factory = factory;
    }

    public override async Task<PaymentActionResponse> CreatePayment(CreatePaymentRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return Fail("Invalid orderId");

        var cmd = new CreatePaymentCommand(orderId, request.AmountCents, request.Currency);
        var result = await _mediator.Send(cmd, context.CancellationToken);
        return Map(result);
    }

    public override async Task<PaymentActionResponse> StartPayment(Shared.Proto.StartPaymentRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return Fail("Invalid orderId");

        var cmd = new StartPaymentCommand(orderId, request.Provider, request.Capture);
        var result = await _mediator.Send(cmd, context.CancellationToken);
        if (result.Success && result.Data is not null && !string.IsNullOrWhiteSpace(result.Data.PaymentUrl))
            return new PaymentActionResponse { Success = true, Message = result.Data.PaymentUrl };

        return Map(result);
    }

    public override async Task<PaymentActionResponse> CapturePayment(CapturePaymentRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return Fail("Invalid orderId");

        long? amount = request.AmountCents > 0 ? request.AmountCents : null;
        var cmd = new CapturePaymentCommand(orderId, amount);
        var result = await _mediator.Send(cmd, context.CancellationToken);
        return Map(result);
    }

    public override async Task<PaymentActionResponse> CancelPayment(CancelPaymentRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return Fail("Invalid orderId");

        var cmd = new CancelPaymentCommand(orderId);
        var result = await _mediator.Send(cmd, context.CancellationToken);
        return Map(result);
    }

    public override async Task<PaymentActionResponse> RefundPayment(RefundPaymentRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return Fail("Invalid orderId");

        var cmd = new RefundPaymentCommand(orderId, request.AmountCents);
        var result = await _mediator.Send(cmd, context.CancellationToken);
        return Map(result);
    }

    public override async Task<GetPaymentStatusResponse> GetPaymentStatus(GetPaymentStatusRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out var orderId))
            return new GetPaymentStatusResponse { Found = false };

        await using var uow = _factory.Create(orderId);
        var payment = await uow.Payments.GetByOrderId(orderId, context.CancellationToken);
        if (payment is null)
            return new GetPaymentStatusResponse { Found = false };

        return new GetPaymentStatusResponse
        {
            Found = true,
            Status = payment.Status.ToString(),
            PaymentId = payment.Id.ToString(),
            Provider = payment.Provider ?? string.Empty,
            ExternalPaymentId = payment.ExternalPaymentId ?? string.Empty,
            PaymentUrl = payment.PaymentUrl ?? string.Empty
        };
    }

    private static PaymentActionResponse Map(ApiResponse response)
    {
        var message = response.Message;
        if (string.IsNullOrWhiteSpace(message) && response.Errors is { Count: > 0 })
            message = string.Join("; ", response.Errors);

        return new PaymentActionResponse
        {
            Success = response.Success,
            Message = message ?? string.Empty
        };
    }

    private static PaymentActionResponse Map<T>(ApiResponse<T> response)
    {
        var message = response.Message;
        if (string.IsNullOrWhiteSpace(message) && response.Errors is { Count: > 0 })
            message = string.Join("; ", response.Errors);

        return new PaymentActionResponse
        {
            Success = response.Success,
            Message = message ?? string.Empty
        };
    }

    private static PaymentActionResponse Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
