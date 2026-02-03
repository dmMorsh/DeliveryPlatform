using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

public class AuthController : Controller
{
    private readonly AuthApiClient _api;

    public AuthController(AuthApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        var token = await _api.LoginAsync(model, ct);

        if (token == null)
        {
            ModelState.AddModelError("", "Invalid login");
            return View(model);
        }

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // true если https
            SameSite = SameSiteMode.Strict
        });

        // Создаем identity для MVC
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, model.Email),
            new("jwt", token)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(principal);
        
        return RedirectToAction("Index", "Catalog");
    }

    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("access_token");
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }
}
