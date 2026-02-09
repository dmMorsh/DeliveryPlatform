using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[Route("orders")]
public class OrdersController : Controller
{
    private readonly OrderApiClient _api;
    private readonly PaymentApiClient _paymentApi;
    private readonly IConfiguration _config;

    public OrdersController(OrderApiClient api, PaymentApiClient paymentApi, IConfiguration config)
    {
        _api = api;
        _paymentApi = paymentApi;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var orders = await _api.GetMyOrdersAsync(ct);
        return View(orders ?? new List<OrderViewModel>());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var order = await _api.GetByIdAsync(id, ct);

        if (order == null)
            return NotFound();

        var payment = await _paymentApi.GetStatusAsync(order.Id, ct);
        if (payment != null)
        {
            order.PaymentId = payment.PaymentId;
            order.PaymentStatus = payment.Status;
            order.PaymentProvider = payment.Provider;
            order.PaymentUrl = payment.PaymentUrl;
        }

        return View(order);
    }

    [HttpPost("{id:guid}/pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
    {
        var provider = _config["Payments:DefaultProvider"];
        if (string.IsNullOrWhiteSpace(provider))
            provider = "YooMoney";

        var payment = await _paymentApi.GetStatusAsync(id, ct);
        if (payment is null)
        {
            TempData["PaymentError"] = "Payment is not ready yet.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.Equals(payment.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(payment.Status, "Authorized", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(payment.PaymentUrl))
                return Redirect(payment.PaymentUrl);
        }

        if (!string.Equals(payment.Status, "Created", StringComparison.OrdinalIgnoreCase))
        {
            TempData["PaymentError"] = $"Payment status is {payment.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var start = await _paymentApi.StartPaymentAsync(id, provider, capture: true, ct);
        if (start is null || string.IsNullOrWhiteSpace(start.PaymentUrl))
        {
            TempData["PaymentError"] = "Failed to start payment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return Redirect(start.PaymentUrl);
    }
}
