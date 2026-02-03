using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;


[Authorize]
public class CartController : Controller
{
    private readonly CartApiClient _api;

    public CartController(CartApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await _api.GetCartAsync(ct);
        return View(cart);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid productId, string name, long priceCents,
        int qty = 1, CancellationToken ct = default)
    {
        await _api.AddAsync(productId, name, priceCents, qty, ct);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(Guid productId, CancellationToken ct)
    {
        await _api.RemoveAsync(productId, ct);
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public IActionResult Checkout(long costCents, CancellationToken ct)
    {
        return View(new CheckoutViewModel{CostCents = costCents});
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(
        CheckoutViewModel model,
        CancellationToken ct)
    {
        var orderId = await _api.CheckoutAsync(model, ct);

        if (orderId == null)
        {
            ModelState.AddModelError("", "Checkout failed");
            return View(model);
        }

        return Redirect($"/orders/{orderId}");
    }

}
