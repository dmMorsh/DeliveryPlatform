using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Aggregates;

public enum OrderStatus
{
    Pending,
    Reserved,
    Confirmed,
    Assigning,
    Assigned,
    InDelivery,
    Delivered,
    Cancelled,
    Failed
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

        if (newStatus == OrderStatus.Reserved && prev != OrderStatus.Pending)
            throw new Exception($"previous status must be pending. previous status is {prev.ToString()}");
        
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
        string? currency,
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
            CostCents = new Money(costCents, string.IsNullOrWhiteSpace(currency) ? "USD" : currency),
            CourierNote = courierNote,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            _items = items
        };

        order.AddDomainEvent(new OrderCreatedDomainEvent {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            FromAddress = order.From.Street,
            ToAddress = order.To.Street,
            CostCents = order.CostCents.AmountCents,
            Description = order.Description,
            Items = order.Items.Select(i=> new DomainOrderItemSnapshot
            {
                ProductId =  i.ProductId, 
                Name = i.Name,
                PriceCents = i.PriceCents,
                Quantity = i.Quantity,
            }).ToList(),
        });

        return order;
    }

    public void MarkItemsReserved(IReadOnlyCollection<OrderItem> items)
    {
        foreach (var item in items)
        {
            if (item.Status != OrderItemStatus.Pending)
                continue;

            item.MarkReserved();
        }

        if (Items.All(i => i.Status == OrderItemStatus.Reserved))
            ChangeStatus(OrderStatus.Reserved);
    }

    public void MarkItemsReleasing(IReadOnlyCollection<OrderItem> items)
    {
        foreach (var item in items)
        {
            if (item.Status is OrderItemStatus.Releasing)
                continue;

            item.MarkReleasing();
        }
    }

    public void MarkItemsFailed(OrderItem[] items)
    {
        foreach (var item in items)
        {
            if (item.Status is OrderItemStatus.ReservationFailed)
                continue;

            item.MarkReservationFailed();
        }

        ChangeStatus(OrderStatus.Failed);

        if (Items.Any(i => i.Status is OrderItemStatus.Reserved))
        {
            AddDomainEvent(new OrderItemsReleaseDomainEvent
            {
                OrderId = Id,
                Items = Items.Where(i => i.Status is OrderItemStatus.Reserved)
                    .Select(i => new DomainOrderItemSnapshot
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                    }).ToArray()
            });
            Items.Where(i => i.Status is OrderItemStatus.Reserved)
                .ToList()
                .ForEach(i => i.MarkReleasing());
        }
    }

    public void MarkAsInconsistent(string error)
    {
        Description += Environment.NewLine + error;
        ChangeStatus(OrderStatus.Failed);
        AddDomainEvent(new OrderCriticalErrorDomainEvent
        {
            OrderId = Id, 
            ClientId = ClientId, 
            Description = Description
        });
    }
}
