using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.StartPayment;

public class StartPaymentCommandHandler : IRequestHandler<StartPaymentCommand, ApiResponse<StartPaymentResult>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentStatusCheckScheduler _statusScheduler;

    public StartPaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentStatusCheckScheduler statusScheduler)
    {
        _factory = factory;
        _providers = providers;
        _statusScheduler = statusScheduler;
    }

    public async Task<ApiResponse<StartPaymentResult>> Handle(StartPaymentCommand request, CancellationToken cancellationToken)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, cancellationToken);

        if (payment is null)
            return ApiResponse<StartPaymentResult>.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

        if (payment.Status != PaymentStatus.Ready)
            return ApiResponse<StartPaymentResult>.ErrorResponse(ErrorCodes.Invariant, "Payment status is not valid");

        var provider = _providers.Get(request.Provider);

        var locked = await uow.Payments.TryMarkStartingAsync(payment.OrderId, cancellationToken);
        if (!locked)
            return ApiResponse<StartPaymentResult>.ErrorResponse(ErrorCodes.Conflict, "Payment is already being processed");

        var requestModel = new StartPaymentRequest(
            payment.Id,
            payment.OrderId,
            payment.AmountCents,
            payment.Currency,
            $"Order {payment.OrderId}",
            request.Capture);

        try
        {
            var result = await provider.StartPayment(requestModel, cancellationToken);

            payment = await uow.Payments.GetByOrderId(request.OrderId, cancellationToken);
            if (payment is null)
                return ApiResponse<StartPaymentResult>.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

            payment.Start(provider.Name, result.ExternalPaymentId, result.PaymentUrl);
            await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, result.ExternalPaymentId, provider.Name, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            _statusScheduler.ScheduleStatusCheck(payment.OrderId);
            
            return ApiResponse<StartPaymentResult>.SuccessResponse(result);
        }
        catch (Exception)
        {
            var current = await uow.Payments.GetByOrderId(request.OrderId, cancellationToken);
            if (current is not null && current.Status == PaymentStatus.Starting)
            {
                current.MarkReady();
                await uow.SaveChangesAsync(cancellationToken);
            }
            throw;
        }
    }
}
