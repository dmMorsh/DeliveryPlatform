using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderReadService.Application.Interfaces;
using OrderReadService.Infrastructure.Inbox;
using OrderReadService.Infrastructure.Persistence;
using OrderReadService.Infrastructure.Services;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

var readStoreConnectionString = configuration.GetConnectionString("OrderReadDb") ??
                                Environment.GetEnvironmentVariable("ORDER_READ_DB") ??
                                "Host=localhost;Database=order_read;Username=postgres;Password=postgres";

services.AddDbContext<OrderReadDbContext>(options =>
    options.UseNpgsql(readStoreConnectionString));

services.AddScoped<OrderReadProjector>();
services.AddScoped<IEventInbox, OrderReadEventInbox>();
// services.AddScoped<OrderReadProjectionConsumer>();
// services.AddScoped<IEventConsumer>(sp => sp.GetRequiredService<OrderReadProjectionConsumer>());

builder.Services.AddSingleton<OrderReadProjectionConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<OrderReadProjectionConsumer>>();

services.AddControllers();
services.AddEndpointsApiExplorer();

// redis cache for order reads
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var conn = sp.GetRequiredService<IConfiguration>().GetValue<string>("Redis:Connection")
               ?? "localhost";
    return ConnectionMultiplexer.Connect(conn);
});
services.AddSingleton<IOrderReadCache, OrderReadRedisCache>();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();