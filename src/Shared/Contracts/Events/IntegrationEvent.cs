namespace Shared.Contracts.Events;

public abstract record IntegrationEvent
{
    /// <summary>Timestamp события</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>Тип события</summary>
    public abstract string EventType { get; }
    
    /// <summary>Уникальный идентификатор события</summary>
    public string EventId { get; } = Guid.NewGuid().ToString();
    
    
    /// <summary>Версия события для evolving schema</summary>
    public abstract int Version { get; }
    
    /// <summary>Тип корневого агрегата (Order, Courier, etc.)</summary>
    public abstract string AggregateType { get; }
    
    /// <summary>ID корневого агрегата</summary>
    public abstract Guid AggregateId { get; }
}