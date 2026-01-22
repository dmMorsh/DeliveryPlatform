using Mapster;
using Shared.Proto;

namespace OrderService.Api.Mappings;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;

        // TypeAdapterConfig<ProtoOrderItem, DomainOrderItem>
        //     .NewConfig()
        //     .ConstructUsing(src => 
        //         new DomainOrderItem(
        //             Guid.Parse(src.ProductId), 
        //             src.Name, 
        //             src.Price, 
        //             src.Quantity));
        // TypeAdapterConfig<DomainOrderItem, DomainOrderItem>.NewConfig()
        //     .MapWith(src => src);

        // Map gRPC request -> application DTO
        TypeAdapterConfig<CreateOrderRequest, Application.CreateOrderModel>
            .NewConfig()
            .Map(dest => dest.ClientId, src => Guid.Parse(src.CustomerId))
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
