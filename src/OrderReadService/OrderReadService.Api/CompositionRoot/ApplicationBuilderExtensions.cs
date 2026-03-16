using Microsoft.EntityFrameworkCore;
using OrderReadService.Infrastructure.Persistence;

namespace OrderReadService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapOrderReadServiceHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        return endpoints;
    }

    public static void RunOrderReadServiceMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderReadDbContext>();
        dbContext.Database.Migrate();
    }
}
