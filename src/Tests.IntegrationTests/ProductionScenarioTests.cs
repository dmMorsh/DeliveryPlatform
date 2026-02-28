using CartService.Domain.Aggregates;
using DeliveryService.Domain.Aggregates;
using DeliveryService.Domain.Events;
using FluentAssertions;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Mapping;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using PaymentService.Domain.Aggregates;
using PaymentService.Domain.SeedWork;

namespace Tests.IntegrationTests;

public class ProductionScenarioTests
{
    [Fact]
    public void Order_MarkItemsReserved_OnlyReservedWhenAllItemsReserved()
    {
        var items = new List<OrderItem>
        {
            new(Guid.NewGuid(), "Item1", 100, 1),
            new(Guid.NewGuid(), "Item2", 200, 2)
        };

        var order = Order.Create(
            orderId:Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2,
            description: "test",
            weightGrams: 10,
            costCents: 300,
            currency: "USD",
            courierNote: null,
            items: items);

        order.Status.Should().Be(OrderStatus.Pending);

        order.MarkItemsReserved(new[] { items[0] });
        order.Status.Should().Be(OrderStatus.Pending);

        order.MarkItemsReserved(new[] { items[1] });
        order.Status.Should().Be(OrderStatus.Reserved);
    }

    [Fact]
    public void Order_InvalidTransition_DoesNotChangeStatus()
    {
        var items = new List<OrderItem>
        {
            new(Guid.NewGuid(), "Item1", 100, 1)
        };

        var order = Order.Create(
            orderId:Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2,
            description: "test",
            weightGrams: 10,
            costCents: 100,
            currency: "USD",
            courierNote: null,
            items: items);

        order.ChangeStatus(OrderStatus.Delivered);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Payment_StartRequiresStartingStatus()
    {
        var payment = Payment.Create(Guid.NewGuid(), 100, "USD");

        var act = () => payment.Start("provider", "ext", "url");
        act.Should().Throw<DomainException>();

        payment.MarkReady();
        payment.MarkStarting();
        payment.Start("provider", "ext", "url");
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Cart_Checkout_SetsLastOrderIdAndResetsCheckoutId()
    {
        var cart = new Cart(Guid.NewGuid());
        cart.AddItem(Guid.NewGuid(), "Item", 100, 1);
        var oldCheckoutId = cart.CheckoutId;
        var orderId = Guid.NewGuid();

        cart.Checkout(orderId);

        cart.Items.Should().BeEmpty();
        cart.CheckoutId.Should().NotBe(oldCheckoutId);
    }

    [Fact]
    public void OrderFactory_UsesCheckoutIdAsOrderId()
    {
        var checkoutId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            ClientId: Guid.NewGuid(),
            FromAddress: "A",
            ToAddress: "B",
            FromLatitude: 1,
            FromLongitude: 1,
            ToLatitude: 2,
            ToLongitude: 2,
            Description: "test",
            WeightGrams: 10,
            CostCents: 100,
            Currency: "USD",
            CourierNote: null,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Item", 100, 1)
            },
            CheckoutId: checkoutId, 
            DesiredReadyAt: null);

        var order = OrderFactory.CreateNew(command);
        order.Id.Should().Be(checkoutId);
    }

    [Fact]
    public void Order_AssignCourier_OnlyWhenConfirmedOrAssigning()
    {
        var items = new List<OrderItem>
        {
            new(Guid.NewGuid(), "Item1", 100, 1)
        };

        var order = Order.Create(
            orderId:Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2,
            description: "test",
            weightGrams: 10,
            costCents: 100,
            currency: "USD",
            courierNote: null,
            items: items);

        var courierId = Guid.NewGuid();
        order.AssignCourier(courierId);
        order.CourierId.Should().BeNull();

        order.ChangeStatus(OrderStatus.Reserved);
        order.ChangeStatus(OrderStatus.Confirmed);
        order.AssignCourier(courierId);
        order.CourierId.Should().Be(courierId);
        order.Status.Should().Be(OrderStatus.Assigned);
    }

    [Fact]
    public void Order_Cancelled_RequestsStockReleaseForReservedItems()
    {
        var items = new List<OrderItem>
        {
            new(Guid.NewGuid(), "Item1", 100, 1),
            new(Guid.NewGuid(), "Item2", 200, 1)
        };

        var order = Order.Create(
            orderId:Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2,
            description: "test",
            weightGrams: 10,
            costCents: 300,
            currency: "USD",
            courierNote: null,
            items: items);

        order.MarkItemsReserved(items);
        order.Status.Should().Be(OrderStatus.Reserved);

        order.Cancel("test");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderItemsReleaseDomainEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Delivery_AssignAcceptFlow_ProducesEventsAndSetsStatus()
    {
        var delivery = Delivery.Create(
            orderId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2);

        delivery.StartAssignment();
        delivery.Status.Should().Be(DeliveryStatus.Assigning);

        var courierId = Guid.NewGuid();
        delivery.OfferToCourier(courierId, DateTime.UtcNow.AddMinutes(1));
        delivery.AcceptOffer(courierId);

        delivery.Status.Should().Be(DeliveryStatus.Assigned);
        delivery.CourierId.Should().Be(courierId);
        delivery.DomainEvents.OfType<DeliveryAssignedDomainEvent>().Should().HaveCount(1);
        delivery.DomainEvents.OfType<DeliveryAcceptedDomainEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Delivery_InvalidAccept_Throws()
    {
        var delivery = Delivery.Create(
            orderId: Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2);

        var act = () => delivery.AcceptOffer(Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Payment_MarkCaptured_IsIdempotent()
    {
        var payment = Payment.Create(Guid.NewGuid(), 100, "USD");
        payment.MarkReady();
        payment.MarkStarting();
        payment.Start("provider", "ext", "url");

        payment.MarkCaptured("ext1");
        payment.Status.Should().Be(PaymentStatus.Captured);

        payment.MarkCaptured("ext2");
        payment.Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public void Order_Cancel_IsIdempotent()
    {
        var items = new List<OrderItem>
        {
            new(Guid.NewGuid(), "Item1", 100, 1)
        };

        var order = Order.Create(
            orderId:Guid.NewGuid(),
            clientId: Guid.NewGuid(),
            fromAddress: "A",
            toAddress: "B",
            fromLatitude: 1,
            fromLongitude: 1,
            toLatitude: 2,
            toLongitude: 2,
            description: "test",
            weightGrams: 10,
            costCents: 100,
            currency: "USD",
            courierNote: null,
            items: items);

        order.Cancel("first");
        order.Status.Should().Be(OrderStatus.Cancelled);
        var eventsAfterFirst = order.DomainEvents.Count;

        order.Cancel("second");
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Count.Should().Be(eventsAfterFirst);
    }
}
