using Mapster;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using CreateOrderItemDto = OrderService.Application.Commands.CreateOrder.CreateOrderItemDto;
using CreateOrderRequest = OrderService.Api.Contracts.CreateOrderRequest;
using DomainOrderItem = OrderService.Domain.Entities.OrderItem;
using SharedOrderItem = Shared.Proto.OrderItem;

namespace OrderService.Api.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

        TypeAdapterConfig<SharedOrderItem, CreateOrderItemDto>
            .NewConfig()
            .Map(dest => dest.ProductId, src => Guid.Parse(src.ProductId))
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PriceCents, src => src.PriceCents);
        
        TypeAdapterConfig<SharedOrderItem, CreateOrderItemDto>
            .NewConfig()
            .Map(dest => dest.ProductId, src => Guid.Parse(src.ProductId))
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PriceCents, src => src.PriceCents);
        
        // Map gRPC request -> application DTO (keeping for backward compatibility)
        TypeAdapterConfig<Shared.Proto.CreateOrderRequest, CreateOrderRequest>
            .NewConfig()
            .Map(dest => dest.ClientId, src => Guid.Parse(src.CustomerId))
            .Map(dest => dest.Items, src => src.Items)
            .Map(dest => dest.FromAddress, src => src.FromAddress)
            .Map(dest => dest.ToAddress, src => src.ToAddress)
            .Map(dest => dest.FromLatitude, src => src.FromLatitude)
            .Map(dest => dest.FromLongitude, src => src.FromLongitude)
            .Map(dest => dest.ToLatitude, src => src.ToLatitude)
            .Map(dest => dest.ToLongitude, src => src.ToLongitude)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.WeightGrams, src => src.WeightGrams)
            .Map(dest => dest.CostCents, src => src.CostCents)
            .Map(dest => dest.CourierNote, src => src.CourierNote)
            .Map(dest => dest.CheckoutId, src => string.IsNullOrWhiteSpace(src.CheckoutId) ? (Guid?)null : Guid.Parse(src.CheckoutId));

        // Map API request -> command
        TypeAdapterConfig<CreateOrderRequest, CreateOrderCommand>
            .NewConfig()
            .Map(dest => dest.ClientId, src => src.ClientId)
            .Map(dest => dest.Items, src => src.Items)
            .Map(dest => dest.FromAddress, src => src.FromAddress)
            .Map(dest => dest.ToAddress, src => src.ToAddress)
            .Map(dest => dest.FromLatitude, src => src.FromLatitude)
            .Map(dest => dest.FromLongitude, src => src.FromLongitude)
            .Map(dest => dest.ToLatitude, src => src.ToLatitude)
            .Map(dest => dest.ToLongitude, src => src.ToLongitude)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.WeightGrams, src => src.WeightGrams)
            .Map(dest => dest.CostCents, src => src.CostCents)
            .Map(dest => dest.Currency, src => src.Currency)
            .Map(dest => dest.CourierNote, src => src.CourierNote)
            .Map(dest => dest.CheckoutId, src => src.CheckoutId)
            .Map(dest => dest.DesiredReadyAt, src => src.DesiredReadyAt);

        // Map gRPC request -> command
        TypeAdapterConfig<Shared.Proto.CreateOrderRequest, CreateOrderCommand>
            .NewConfig()
            .Map(dest => dest.ClientId, src => Guid.Parse(src.CustomerId))
            .Map(dest => dest.Items, src => src.Items)
            .Map(dest => dest.FromAddress, src => src.FromAddress)
            .Map(dest => dest.ToAddress, src => src.ToAddress)
            .Map(dest => dest.FromLatitude, src => src.FromLatitude)
            .Map(dest => dest.FromLongitude, src => src.FromLongitude)
            .Map(dest => dest.ToLatitude, src => src.ToLatitude)
            .Map(dest => dest.ToLongitude, src => src.ToLongitude)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.WeightGrams, src => src.WeightGrams)
            .Map(dest => dest.CostCents, src => src.CostCents)
            .Map(dest => dest.Currency, src => src.Currency ?? string.Empty)
            .Map(dest => dest.CourierNote, src => src.CourierNote)
            .Map(dest => dest.CheckoutId, src => string.IsNullOrWhiteSpace(src.CheckoutId) ? (Guid?)null : Guid.Parse(src.CheckoutId))
            .Map(dest => dest.DesiredReadyAt, src => (DateTime?)null);

        
        TypeAdapterConfig<DomainOrderItem, OrderViewItem>
            .NewConfig()
            .Map(dest => dest.ProductId, src => src.ProductId)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PriceCents, src => src.PriceCents);
        TypeAdapterConfig<Order, OrderView>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.OrderNumber, src => src.OrderNumber)
            .Map(dest => dest.ClientId, src => src.ClientId)
            .Map(dest => dest.CourierId, src => src.CourierId)
            .Map(dest => dest.FromAddress, src => src.From.Street)
            .Map(dest => dest.ToAddress, src => src.To.Street)
            .Map(dest => dest.FromLatitude, src => src.From.Latitude)
            .Map(dest => dest.FromLongitude, src => src.From.Longitude)
            .Map(dest => dest.ToLatitude, src => src.To.Latitude)
            .Map(dest => dest.ToLongitude, src => src.To.Longitude)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.WeightGrams, src => src.WeightGrams)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.CostCents, src => src.CostCents.AmountCents)
            .Map(dest => dest.Currency, src => src.CostCents.Currency)
            .Map(dest => dest.CourierNote, src => src.CourierNote)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.AssignedAt, src => src.AssignedAt)
            .Map(dest => dest.DeliveredAt, src => src.DeliveredAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.ReadyAt, src => src.ReadyAt)
            .Map(dest => dest.IsReadyForDelivery, src => src.IsReadyForDelivery)
            .Map(dest => dest.AcceptedAt, src => src.AcceptedAt)
            .Map(dest => dest.RejectedAt, src => src.RejectedAt)
            .Map(dest => dest.RejectionReason, src => src.RejectionReason)
            .Map(dest => dest.ExpectedReadyAt, src => src.ExpectedReadyAt)
            .Map(dest => dest.KitchenSlotStart, src => src.KitchenSlotStart)
            .Map(dest => dest.KitchenDelayedNotifiedAt, src => src.KitchenDelayedNotifiedAt)
            .Map(dest => dest.DeliveryZoneId, src => src.DeliveryZoneId)
            .Map(dest => dest.DeliveryZoneName, src => src.DeliveryZoneName)
            .Map(dest => dest.DeliveryZoneDistanceKm, src => src.DeliveryZoneDistanceKm)
            .Map(dest => dest.DeliveryPickupSlaMinutes, src => src.DeliveryPickupSlaMinutes)
            .Map(dest => dest.DeliveryTransitSlaMinutes, src => src.DeliveryTransitSlaMinutes)
            .Map(dest => dest.DeliveryFeeMultiplier, src => src.DeliveryFeeMultiplier)
            .Map(dest => dest.Items, src => src.Items);
    }
}
