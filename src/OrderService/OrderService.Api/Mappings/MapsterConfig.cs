using Mapster;
using OrderService.Application.Models;
using Shared.Proto;

namespace OrderService.Api.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

        TypeAdapterConfig<OrderItem, CreateOrderItemModel>
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
    }
}
