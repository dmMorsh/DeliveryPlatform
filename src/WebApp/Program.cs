using Microsoft.AspNetCore.Authentication.Cookies;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
    }).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
});

builder.Services.AddHttpClient<CartApiClient>(c =>
    {
        c.BaseAddress = new Uri("http://localhost:5136");
    }).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<OrderApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    }).AddHttpMessageHandler<AuthTokenHandler>();

builder.Services.AddHttpClient<InventoryApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5136"); // Gateway
    }).AddHttpMessageHandler<AuthTokenHandler>();

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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();