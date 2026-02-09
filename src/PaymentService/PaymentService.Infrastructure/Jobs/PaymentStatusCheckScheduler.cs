using Hangfire;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Jobs;

public sealed class PaymentStatusCheckScheduler : IPaymentStatusCheckScheduler
{
    private readonly IBackgroundJobClient _jobs;
    private readonly PaymentStatusCheckOptions _options;

    public PaymentStatusCheckScheduler(IBackgroundJobClient jobs, IOptions<PaymentStatusCheckOptions> options)
    {
        _jobs = jobs;
        _options = options.Value;
    }

    public void ScheduleStatusCheck(Guid orderId)
    {
        if (_options.DelaysSeconds.Length == 0)
            return;

        var delaySeconds = _options.DelaysSeconds[0];
        _jobs.Schedule<PaymentStatusCheckJob>(
            job => job.Check(orderId, 0, default),
            TimeSpan.FromSeconds(delaySeconds));
    }
}
