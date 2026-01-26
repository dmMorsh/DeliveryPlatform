using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Aggregates;

public enum OrderStatus
{
    Pending = 0,
    Assigned = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4
}

public class Order : AggregateRoot
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid ClientId { get; private set; }
    public Guid? CourierId { get; private set; }
    public Address From { get; private set; }
    public Address To { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int WeightGrams { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money CostCents { get; private set; }
    public string? CourierNote { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void AssignCourier(Guid courierId)
    {
        if (CourierId.HasValue) return;
        CourierId = courierId;
        AssignedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAssignedDomainEvent { OrderId = Id, CourierId = courierId });
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Assigned;
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        var prev = Status;
        if (prev == newStatus) return;
        Status = newStatus;
        if (newStatus == OrderStatus.Delivered && !DeliveredAt.HasValue)
            DeliveredAt = DateTime.UtcNow;
        AddDomainEvent(new OrderStatusChangedDomainEvent { OrderId = Id, PreviousStatus = prev, NewStatus = newStatus });
    }

    public void AddCourierNote(string note)
    {
        CourierNote = note;
    }

    public static Order Create(
        Guid clientId,
        string fromAddress,
        string toAddress,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        string? description,
        int weightGrams,
        long costCents,
        string? courierNote,
        List<OrderItem>? items = null)
    {
        if (items == null || !items.Any())
            throw new DomainException("Order must contain items");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = Shared.Utilities.OrderNumberGenerator.GenerateOrderNumber(),
            ClientId = clientId,
            From = new Address(fromAddress,fromLatitude,fromLongitude),
            To =  new Address(toAddress,toLatitude,toLongitude),
            Description = description ?? string.Empty,
            WeightGrams = weightGrams,
            CostCents = new Money(costCents, "USD"),
            CourierNote = courierNote,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            _items = items ?? new List<OrderItem>()
        };

        order.AddDomainEvent(new OrderCreatedDomainEvent {
            OrderId = order.Id,
            AggregateId = order.Id,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            FromAddress = order.From.Street,
            ToAddress = order.To.Street,
            CostCents = order.CostCents.Amount,
            Description = order.Description
        });

        return order;
    }
}
