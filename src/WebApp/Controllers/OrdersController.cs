using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[Route("orders")]
public class OrdersController : Controller
{
    private readonly OrderApiClient _api;

    public OrdersController(OrderApiClient api)
    {
        _api = api;
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

        return View(order);
    }
}
