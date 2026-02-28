using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.RefundPayment;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public RefundPaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, ct);
        if (payment is null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

        if (payment.Status != PaymentStatus.Captured)
            return ApiResponse.ErrorResponse(ErrorCodes.Invariant, "Payment status is not valid");

        var provider = _providers.Get(payment.Provider);
        await provider.RefundPayment(payment.ExternalPaymentId, request.AmountCents, payment.Currency, ct);

        var prev = payment.Status;
        payment.MarkRefunded();
        var outbox = new List<OutboxMessage>();
        if (payment.Status != prev)
            outbox.Add(OutboxMessage.From(_eventMapper.MapRefunded(payment)));

        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);

        return ApiResponse.SuccessResponse();
    }
}
