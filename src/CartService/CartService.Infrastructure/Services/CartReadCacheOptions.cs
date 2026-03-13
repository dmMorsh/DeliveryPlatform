namespace CartService.Infrastructure.Services;

public sealed class CartReadCacheOptions
{
    public int TtlSeconds { get; set; } = 3600;
}
