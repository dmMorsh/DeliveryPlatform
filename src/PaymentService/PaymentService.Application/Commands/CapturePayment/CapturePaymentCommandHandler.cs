using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CapturePayment;

public class CapturePaymentCommandHandler : IRequestHandler<CapturePaymentCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public CapturePaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(CapturePaymentCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, ct);
        if (payment is null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

        if (payment.Status is not PaymentStatus.Authorized and not PaymentStatus.Pending)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Payment status is not valid");

        var provider = _providers.Get(payment.Provider);
        await provider.CapturePayment(payment.ExternalPaymentId, request.AmountCents, payment.Currency, ct);

        var prev = payment.Status;
        payment.MarkCaptured(payment.ExternalPaymentId);
        var outbox = new List<OutboxMessage>();
        if (payment.Status != prev)
            outbox.Add(OutboxMessage.From(_eventMapper.MapCaptured(payment)));

        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);

        return ApiResponse.SuccessResponse();
    }
}
