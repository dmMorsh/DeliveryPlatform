namespace GatewayApi.Contracts.Cart;

public record AddItemRequest(Guid ProductId, string Name, int PriceCents, int Quantity);