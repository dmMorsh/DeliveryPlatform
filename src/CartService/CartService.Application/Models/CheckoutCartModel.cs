namespace CartService.Application.Models;

public record CheckoutCartModel(
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