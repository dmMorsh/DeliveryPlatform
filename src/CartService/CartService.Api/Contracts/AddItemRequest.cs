namespace CartService.Api.Contracts;

public record AddItemRequest(Guid ProductId, string Name, int PriceCents, int Quantity);