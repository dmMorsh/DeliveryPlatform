using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessOrderCanceled;

public class ProcessOrderCanceledCommandHandler
    : IRequestHandler<ProcessOrderCanceledCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public ProcessOrderCanceledCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(ProcessOrderCanceledCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, ct);
        if (payment is null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

        if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Cancelled or PaymentStatus.Failed)
            return ApiResponse.SuccessResponse();

        var outbox = new List<OutboxMessage>();
        var prev = payment.Status;

        if (payment.Status == PaymentStatus.Captured)
        {
            if (string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
                return ApiResponse.SuccessResponse();

            var provider = _providers.Get(payment.Provider);
            await provider.RefundPayment(payment.ExternalPaymentId, payment.AmountCents, payment.Currency, ct);
            payment.MarkRefunded();
            if (payment.Status != prev)
                outbox.Add(OutboxMessage.From(_eventMapper.MapRefunded(payment)));
        }
        else if (payment.Status is PaymentStatus.Authorized or PaymentStatus.Pending or PaymentStatus.Starting)
        {
            if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
            {
                var provider = _providers.Get(payment.Provider);
                await provider.CancelPayment(payment.ExternalPaymentId, ct);
            }

            payment.MarkCancelled();
            if (payment.Status != prev)
                outbox.Add(OutboxMessage.From(_eventMapper.MapCancelled(payment)));
        }
        else if (payment.Status is PaymentStatus.Created or PaymentStatus.Ready)
        {
            payment.MarkCancelled();
            if (payment.Status != prev)
                outbox.Add(OutboxMessage.From(_eventMapper.MapCancelled(payment)));
        }

        if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
            await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);

        return ApiResponse.SuccessResponse();
    }
}
