namespace Shared.Contracts.Events;

public abstract record IntegrationEvent
{
    /// <summary>Event timestamp</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Event type</summary>
    public abstract string EventType { get; }
    
    /// <summary>Unique event identifier</summary>
    public string EventId { get; } = Guid.NewGuid().ToString();
    
    
    /// <summary>Event version for evolving schema</summary>
    public abstract int Version { get; }
    
    /// <summary>Root aggregate type (Order, Courier, etc.)</summary>
    public abstract string AggregateType { get; }
    
    /// <summary>Root aggregate ID</summary>
    public abstract Guid AggregateId { get; }
}
