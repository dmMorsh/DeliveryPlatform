namespace Shared.Services;

public interface IHangfireRetryable
{
    Guid CorrelationId { get; }
}