using Mapster;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using Shared.Proto;
using DomainOrderItem = OrderService.Domain.Entities.OrderItem;
using SharedOrderItem = Shared.Proto.OrderItem;

namespace OrderService.Api.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

        TypeAdapterConfig<SharedOrderItem, CreateOrderItemModel>
            .NewConfig()
            .Map(dest => dest.ProductId, src => Guid.Parse(src.ProductId))
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.PriceCents, src => src.PriceCents);
        
        // Map gRPC request -> application DTO
        TypeAdapterConfig<CreateOrderRequest, CreateOrderModel>
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
            .Map(dest => dest.CourierNote, src => src.CourierNote);

        
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
            .Map(dest => dest.Items, src => src.Items);
    }
}
