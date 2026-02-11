using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Shared.Services;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceTelemetry("webapp");
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/auth/login";
        o.AccessDeniedPath = "/auth/login";
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthTokenHandler>();

builder.Services.AddHttpClient<CatalogApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<CatalogApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
})
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<AuthApiClient>>()));

builder.Services.AddHttpClient<CartApiClient>(c =>
    {
        c.BaseAddress = new Uri("http://localhost:5136");
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<CartApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<OrderApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<OrderApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<PaymentApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<PaymentApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<InventoryApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<InventoryApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

var courierServiceUrl = builder.Configuration.GetValue<string>("Services:CourierServiceUrl") ?? "http://localhost:5206";
var deliveryServiceUrl = builder.Configuration.GetValue<string>("Services:DeliveryServiceUrl") ?? "http://localhost:5207";

builder.Services.AddHttpClient<CourierApiClient>(client =>
    {
        client.BaseAddress = new Uri(courierServiceUrl);
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<CourierApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<DeliveryApiClient>(client =>
    {
        client.BaseAddress = new Uri(deliveryServiceUrl);
    })
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<DeliveryApiClient>>()))
    .AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddScoped<ILocationTrackingClient>(sp =>
    new LocationTrackingClientImpl(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));

var app = builder.Build();

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/auth"))
    {
        await next();
        return;
    }

    var token = ctx.Request.Cookies["access_token"];
    if (!string.IsNullOrWhiteSpace(token))
    {
        var expired = false;
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            expired = jwt.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            expired = true;
        }

        if (expired)
        {
            ctx.Response.Cookies.Delete("access_token");
            await ctx.SignOutAsync();
            var returnUrl = ctx.Request.Path + ctx.Request.QueryString;
            ctx.Response.Redirect($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }
    }

    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
