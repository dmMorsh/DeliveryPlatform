namespace InventoryService.Application.Interfaces;

public interface IHangfireRetryable
{
    Guid CorrelationId { get; }
}
