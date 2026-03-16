using OrderReadService.Api.CompositionRoot;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// API surface
builder.Services.AddOrderReadServiceApi();

// Composition root
var settings = OrderReadServiceSettings.From(builder.Configuration, builder.Environment);
builder.Services.AddOrderReadServiceCore(settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddOrderReadServiceHealthChecks(
    settings);

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Migration
app.RunOrderReadServiceMigrations();

// Endpoints
app.MapOrderReadServiceHealthChecks();
app.MapControllers();

app.Run();
