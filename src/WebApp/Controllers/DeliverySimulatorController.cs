using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

public class DeliverySimulatorController : Controller
{
    private readonly CourierApiClient _courierApi;
    private readonly DeliveryApiClient _deliveryApi;
    private readonly ILocationTrackingClient _locationClient;

    public DeliverySimulatorController(
        CourierApiClient courierApi,
        DeliveryApiClient deliveryApi,
        ILocationTrackingClient locationClient)
    {
        _courierApi = courierApi;
        _deliveryApi = deliveryApi;
        _locationClient = locationClient;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new DeliverySimulatorViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterCourier(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _courierApi.RegisterAsync(model.Register, ct);
        TempData["Result"] = FormatResult(res, "Courier registered");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCourier(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _courierApi.UpdateAsync(model.CourierUpdate, ct);
        TempData["Result"] = FormatResult(res, "Courier updated");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> LookupDelivery(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        if (model.DeliveryLookup.DeliveryId.HasValue)
        {
            var res = await _deliveryApi.GetByIdAsync(model.DeliveryLookup.DeliveryId.Value, ct);
            TempData["Result"] = FormatResult(res, "Delivery fetched");
        }
        else if (model.DeliveryLookup.OrderId.HasValue)
        {
            var res = await _deliveryApi.GetByOrderAsync(model.DeliveryLookup.OrderId.Value, ct);
            TempData["Result"] = FormatResult(res, "Delivery fetched");
        }
        else
        {
            TempData["Result"] = "Provide deliveryId or orderId";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Accept(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.AcceptAsync(GetDeliveryId(model), model.DeliveryAction.CourierId, ct);
        TempData["Result"] = FormatResult(res, "Accepted");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Decline(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.DeclineAsync(GetDeliveryId(model), model.DeliveryAction.CourierId, model.DeliveryAction.Reason, ct);
        TempData["Result"] = FormatResult(res, "Declined");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> PickUp(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.PickUpAsync(GetDeliveryId(model), model.DeliveryAction.CourierId, ct);
        TempData["Result"] = FormatResult(res, "Picked up");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Start(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.StartAsync(GetDeliveryId(model), model.DeliveryAction.CourierId, ct);
        TempData["Result"] = FormatResult(res, "In transit");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Complete(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.CompleteAsync(GetDeliveryId(model), model.DeliveryAction, ct);
        TempData["Result"] = FormatResult(res, "Completed");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Fail(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.FailAsync(GetDeliveryId(model), model.DeliveryAction.Reason, ct);
        TempData["Result"] = FormatResult(res, "Failed");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Return(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.ReturnAsync(GetDeliveryId(model), model.DeliveryAction.Reason, ct);
        TempData["Result"] = FormatResult(res, "Returned");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var res = await _deliveryApi.CancelAsync(GetDeliveryId(model), model.DeliveryAction.Reason, ct);
        TempData["Result"] = FormatResult(res, "Cancelled");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateLocation(DeliverySimulatorViewModel model, CancellationToken ct)
    {
        var ok = await _locationClient.UpdateCourierLocationAsync(
            model.LocationUpdate.CourierId,
            model.LocationUpdate.Latitude,
            model.LocationUpdate.Longitude,
            model.LocationUpdate.Accuracy);

        TempData["Result"] = ok ? "Location updated" : "Location update failed";
        return RedirectToAction(nameof(Index));
    }

    private static string FormatResult(object? response, string okMessage)
    {
        if (response == null)
            return "No response";

        var prop = response.GetType().GetProperty("Success");
        if (prop != null && prop.PropertyType == typeof(bool) && (bool)prop.GetValue(response)! )
            return okMessage;

        return response.ToString() ?? "Failed";
    }

    private static Guid GetDeliveryId(DeliverySimulatorViewModel model)
    {
        if (model.DeliveryAction.DeliveryId.HasValue)
            return model.DeliveryAction.DeliveryId.Value;
        throw new InvalidOperationException("DeliveryId required");
    }
}
