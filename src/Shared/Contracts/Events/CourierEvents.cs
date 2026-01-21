namespace Shared.Contracts.Events;

/// <summary>
/// Event: Курьер создан/зарегистрирован
/// </summary>
public class CourierRegisteredEvent : IntegrationEvent
{
    public Guid CourierId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public override string EventType => "courier.registered";
    public override int Version => 1;
    public override string AggregateType => "courier";
    public override Guid AggregateId => CourierId;
}

/// <summary>
/// Event: Статус курьера изменился
/// </summary>
public class CourierStatusChangedEvent : IntegrationEvent
{
    public Guid CourierId { get; set; }
    public int PreviousStatus { get; set; }
    public int NewStatus { get; set; }
    public string? Reason { get; set; }
    
    public override string EventType => "courier.status.changed";
    public override int Version => 1;
    public override string AggregateType => "courier";
    public override Guid AggregateId => CourierId;
}

/// <summary>
/// Event: Местоположение курьера обновлено
/// </summary>
public class CourierLocationUpdatedEvent : IntegrationEvent
{
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? Accuracy { get; set; }
    
    public override string EventType => "courier.location.updated";
    public override int Version => 1;
    public override string AggregateType => "courier";
    public override Guid AggregateId => CourierId;
}

/// <summary>
/// Event: Рейтинг курьера обновлен
/// </summary>
public class CourierRatingUpdatedEvent : IntegrationEvent
{
    public Guid CourierId { get; set; }
    public double NewRating { get; set; }
    public int TotalRatings { get; set; }
    public string? Feedback { get; set; }
    
    public override string EventType => "courier.rating.updated";
    public override int Version => 1;
    public override string AggregateType => "courier";
    public override Guid AggregateId => CourierId;
}
