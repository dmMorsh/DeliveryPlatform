using CatalogReadService.Api.CompositionRoot;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity
builder.AddServiceTelemetry("catalog-read-service");

// API surface
builder.Services.AddCatalogReadServiceApi();

// Composition root
var settings = CatalogReadServiceSettings.From(builder.Configuration, builder.Environment);
builder.Services.AddCatalogReadServiceCore(settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddCatalogReadServiceHealthChecks(
    settings);

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Endpoints
app.MapControllers();
app.MapCatalogReadServiceHealthChecks();

app.Run();
