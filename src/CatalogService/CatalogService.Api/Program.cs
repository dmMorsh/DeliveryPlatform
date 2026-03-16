using CatalogService.Api.CompositionRoot;
using Hangfire;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity
builder.AddServiceTelemetry("catalog-service");

// API surface
builder.Services.AddCatalogServiceApi();

// Runtime flags
var settings = CatalogServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddCatalogServiceCore(builder.Environment, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddCatalogServiceHealthChecks(
    settings);

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Endpoints
app.MapControllers();
app.MapCatalogServiceHealthChecks();

if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

// Startup tasks
app.RunCatalogServiceMigrations(settings.UseInMemory);

app.Run();
