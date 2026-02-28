using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var encoder = HtmlEncoder.Create(UnicodeRanges.BasicLatin);
var store = new ConcurrentDictionary<string, PaymentState>(StringComparer.OrdinalIgnoreCase);

static bool TryUpdateStatus(
    ConcurrentDictionary<string, PaymentState> store,
    string externalPaymentId,
    Func<PaymentState, string?> mapStatus)
{
    while (true)
    {
        if (!store.TryGetValue(externalPaymentId, out var current))
            return false;

        var newStatus = mapStatus(current);
        if (string.IsNullOrWhiteSpace(newStatus) || newStatus == current.Status)
            return true;

        var updated = current with { Status = newStatus };
        if (store.TryUpdate(externalPaymentId, updated, current))
            return true;
    }
}

app.MapPost("/api/fake-payments/start", (HttpContext ctx, FakeStartRequest request) =>
{
    var externalPaymentId = Guid.NewGuid().ToString("N");
    var paymentUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/pay/{externalPaymentId}";

    var state = new PaymentState
    {
        ExternalPaymentId = externalPaymentId,
        PaymentId = request.PaymentId,
        OrderId = request.OrderId,
        AmountCents = request.AmountCents,
        Currency = request.Currency,
        Description = request.Description,
        Capture = request.Capture,
        Status = "pending",
        ReturnUrl = request.ReturnUrl,
        FailUrl = request.FailUrl,
        CreatedAt = DateTime.UtcNow
    };

    store[externalPaymentId] = state;

    return Results.Ok(new FakeStartResponse(externalPaymentId, paymentUrl));
});

app.MapGet("/api/fake-payments/status/{externalPaymentId}", (string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound();

    return Results.Ok(new FakeStatusResponse(state.Status));
});

app.MapPost("/api/fake-payments/capture/{externalPaymentId}", (string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound();

    TryUpdateStatus(store, externalPaymentId, current =>
        current.Status is "authorized" or "pending" ? "succeeded" : null);

    return Results.Ok();
});

app.MapPost("/api/fake-payments/cancel/{externalPaymentId}", (string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound();

    TryUpdateStatus(store, externalPaymentId, current =>
        current.Status is not "succeeded" ? "cancelled" : null);

    return Results.Ok();
});

app.MapPost("/api/fake-payments/refund/{externalPaymentId}", (string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound();

    TryUpdateStatus(store, externalPaymentId, current =>
        current.Status is "succeeded" ? "refunded" : null);

    return Results.Ok();
});

app.MapGet("/pay/{externalPaymentId}", (string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound("Payment not found");

    var status = encoder.Encode(state.Status);
    var amount = state.AmountCents / 100.0m;
    var currency = encoder.Encode(state.Currency);
    var orderId = encoder.Encode(state.OrderId.ToString());
    var description = encoder.Encode(state.Description ?? string.Empty);

    var html = $"""
                 <!DOCTYPE html>
                 <html lang="en">
                 <head>
                   <meta charset="UTF-8" />
                   <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                   <title>FakePay</title>
                   <style>
                     body { "{font-family: Arial, sans-serif; max-width: 720px; margin: 32px auto; padding: 0 16px;}" }
                     .card { "{border: 1px solid #ddd; border-radius: 8px; padding: 16px;}" }
                     .row { "{display: flex; gap: 8px; margin-top: 12px;}" }
                     button { "{padding: 8px 14px;}" }
                   </style>
                 </head>
                 <body>
                   <h2>FakePay</h2>
                   <div class="card">
                     <div><b>Order:</b> {orderId}</div>
                     <div><b>Amount:</b> {amount} {currency}</div>
                     <div><b>Description:</b> {description}</div>
                     <div><b>Status:</b> {status}</div>
                     <form method="post" action="/pay/{externalPaymentId}/action">
                       <div class="row">
                         <button type="submit" name="action" value="pay">Pay</button>
                         <button type="submit" name="action" value="fail">Fail</button>
                         <button type="submit" name="action" value="cancel">Cancel</button>
                       </div>
                     </form>
                   </div>
                 </body>
                 </html>
                 """;

    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/pay/{externalPaymentId}/action", async (HttpContext ctx, string externalPaymentId) =>
{
    if (!store.TryGetValue(externalPaymentId, out var state))
        return Results.NotFound("Payment not found");

    var form = await ctx.Request.ReadFormAsync();
    var action = form["action"].ToString().Trim().ToLowerInvariant();

    TryUpdateStatus(store, externalPaymentId, current =>
    {
        return action switch
        {
            "pay" => current.Capture ? "succeeded" : "authorized",
            "fail" => "failed",
            "cancel" => "cancelled",
            _ => null
        };
    });

    var redirectUrl = action == "pay" ? state.ReturnUrl : state.FailUrl;
    if (!string.IsNullOrWhiteSpace(redirectUrl))
        return Results.Redirect(redirectUrl);

    return Results.Redirect($"/pay/{externalPaymentId}");
});

app.MapGet("/", () => Results.Redirect("/pay"));

app.Run();

internal sealed record FakeStartRequest(
    Guid PaymentId,
    Guid OrderId,
    long AmountCents,
    string Currency,
    string Description,
    bool Capture,
    string ReturnUrl,
    string FailUrl);

internal sealed record FakeStartResponse(string ExternalPaymentId, string PaymentUrl);

internal sealed record FakeStatusResponse(string Status);

internal sealed record PaymentState
{
    public string ExternalPaymentId { get; init; } = string.Empty;
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public long AmountCents { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Capture { get; init; }
    public string Status { get; init; } = "pending";
    public string? ReturnUrl { get; init; }
    public string? FailUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}
