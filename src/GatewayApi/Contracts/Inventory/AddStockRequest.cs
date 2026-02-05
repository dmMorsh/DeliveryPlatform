namespace GatewayApi.Contracts.Inventory;

public record AddStockRequest(Guid ProductId, int Quantity);