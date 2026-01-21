using Serilog;
using GatewayApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console()
       .MinimumLevel.Information());

// Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IProxyService, ProxyService>();

// CORS для всех источников (Development mode)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Gateway API started. Service URLs - OrderService: {OrderService}, CourierService: {CourierService}",
    builder.Configuration["Services:OrderServiceUrl"] ?? "http://localhost:5001",
    builder.Configuration["Services:CourierServiceUrl"] ?? "http://localhost:5002");

app.Run();
