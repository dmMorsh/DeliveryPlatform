using Hangfire;
using InventoryService.Api.CompositionRoot;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity + logging
builder.AddServiceTelemetry("inventory-service");
builder.UseExtededSerilog();

// API surface
builder.Services.AddInventoryServiceApi();

// Runtime flags
var settings = InventoryServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddInventoryServiceCore(settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddInventoryServiceHealthChecks(
    settings);

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Endpoints
app.MapControllers();
app.MapInventoryServiceHealthChecks();

// Startup tasks
app.RunInventoryServiceMigrationsAndJobs(settings.UseInMemory, builder.Configuration);

if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

app.Run();
