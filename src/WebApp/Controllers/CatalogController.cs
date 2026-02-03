using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[Authorize]
public class CatalogController : Controller
{
    private readonly CatalogApiClient _api;

    public CatalogController(CatalogApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _api.GetProductsAsync(ct);
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var product = await _api.GetByIdAsync(id, ct);
        return View(product);
    }
    
    [HttpGet]
    public IActionResult AddItem(CancellationToken ct = default)
    {
        return View(new ProductViewModel());
    }
    [HttpPost]
    public async Task<IActionResult> Add(ProductViewModel model, CancellationToken ct = default)
    {
        await _api.AddAsync(model, ct);
        return RedirectToAction("Index");
    }

}