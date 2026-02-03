namespace InventoryService.Infrastructure.Hangfire;

public class ProcessedCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CorrelationId { get; set; }

    public string CommandType { get; set; } = default!;

    public DateTime ProcessedAt { get; set; }
}
