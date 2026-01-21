// namespace Shared.Contracts.Events;
//
// /// <summary>
// /// Базовый контракт для интеграционных событий (DTO для межсервисной коммуникации)
// /// </summary>
// public abstract class IntegrationEvent
// {
//     /// <summary>Уникальный идентификатор события</summary>
//     public string EventId { get; set; } = Guid.NewGuid().ToString();
//     
//     /// <summary>Тип события</summary>
//     public abstract string EventType { get; }
//     
//     /// <summary>Версия события для evolving schema</summary>
//     public abstract int Version { get; }
//     
//     /// <summary>Timestamp события</summary>
//     public DateTime Timestamp { get; set; } = DateTime.UtcNow;
//     
//     /// <summary>Тип корневого агрегата (Order, Courier, etc.)</summary>
//     public abstract string AggregateType { get; }
//     
//     /// <summary>ID корневого агрегата</summary>
//     public abstract Guid AggregateId { get; }
// }