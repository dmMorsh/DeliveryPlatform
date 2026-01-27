namespace CartService.Application.Models;

public record AddItemModel(Guid ProductId, string Name, int PriceCents, int Quantity);