using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.StartPayment;

public class StartPaymentCommandHandler : IRequestHandler<StartPaymentCommand, ApiResponse>
{
    private readonly IPaymentRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IPaymentProvider _providers;

    public async Task<ApiResponse> Handle(StartPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _repo.GetByOrderId(request.OrderId);

        if (payment.Status != PaymentStatus.Created)
            return ApiResponse.ErrorResponse("Payment status is not valid");

        // var provider = _providers.Get(providerName);
        //
        // var result = await provider.StartPayment(
        //     payment.Id,
        //     payment.AmountCents,
        //     payment.Currency
        // );
        // payment.MarkPending(
        //     providerName,
        //     result.ExternalPaymentId
        // );
        //
        // _uow.SaveChangesAsync();
        
        return ApiResponse.SuccessResponse();
    }
}