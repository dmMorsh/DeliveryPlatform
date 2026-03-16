namespace Shared.Contracts;

public sealed class ProcessedCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}