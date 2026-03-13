using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[Authorize]
[Route("admin/catalog")]
public class AdminCatalogController : Controller
{
    private readonly CatalogAdminApiClient _api;

    public AdminCatalogController(CatalogAdminApiClient api)
    {
        _api = api;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _api.GetProductsAsync(ct);
        return View(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var product = await _api.GetByIdAsync(id, ct);
        return View(product);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var product = await _api.GetByIdAsync(id, ct);
        return View(product);
    }

    [HttpGet("add")]
    public IActionResult AddItem()
    {
        return View(new ProductViewModel());
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(ProductViewModel model, CancellationToken ct)
    {
        await _api.AddAsync(model, ct);
        return RedirectToAction("Index");
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductViewModel model, CancellationToken ct)
    {
        model.Id = id;
        await _api.UpdateAsync(model, ct);
        return RedirectToAction("Details", new { id });
    }
}
