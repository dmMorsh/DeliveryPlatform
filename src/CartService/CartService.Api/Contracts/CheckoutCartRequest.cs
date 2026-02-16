namespace CartService.Api.Contracts;

public record CheckoutCartRequest(
    string FromAddress,
    string ToAddress,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    int WeightGrams,
    long CostCents,
    string? Currency,
    string? CourierNote
    );