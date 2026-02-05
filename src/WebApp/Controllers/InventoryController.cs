using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly InventoryApiClient _api;

    public InventoryController(InventoryApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _api.GetStocksAsync(ct);
        return View(products);
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View(new SimpleStockItemViewModel());
    }
    
    [HttpPost]
    public async Task<IActionResult> Add(SimpleStockItemViewModel product, CancellationToken ct = default)
    {
        var models = new []{product};
        await _api.AddStocksAsync(models, ct);
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public IActionResult Details(Guid id, int quantity)
    {
        return View(new SimpleStockItemViewModel{ProductId = id, Quantity = quantity});
    }

    [HttpPost]
    public async Task<IActionResult> Adjust(SimpleStockItemViewModel product, CancellationToken ct = default)
    {
        var models = new []{product};
        await _api.AdjustStocksAsync(models, ct);
        return RedirectToAction("Index");
    }

}
