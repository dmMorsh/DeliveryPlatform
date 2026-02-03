using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using InventoryService.Application;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Utils;
using InventoryService.Infrastructure.Hangfire;
using InventoryService.Infrastructure.Mapping;
using InventoryService.Infrastructure.Outbox;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory)
{
    builder.Services.AddDbContext<InventoryDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{   // DbContext
    var connectionString = builder.Configuration.GetConnectionString("Default") 
                           ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
    builder.Services.AddDbContext<InventoryDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    // Hangfire
    builder.Services.AddHangfire(config =>
        // config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true }));
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default")), 
            new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true })
        );
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<IHangfireCommandExecutor, HangfireCommandExecutor>();
    
    // Sharding
    // builder.Services.AddSingleton<IInventoryDbContextFactory, InventoryDbContextFactory>();
    builder.Services.AddSingleton<IShardResolver>(sp =>
    {
        var shardCount = builder.Configuration.GetValue<int>("ShardCount");
        return new HashShardResolver(shardCount);
    });
    builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
    
    // Outbox processor
    builder.Services.AddHostedService<OutboxProcessor>();
}

builder.Services.AddControllers();
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyRetryBehavior<,>))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));

// builder.Services.AddScoped<IStockItemRepository, StockItemRepository>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
// Event Consumer from OrderService
builder.Services.AddSingleton<OrderEventConsumer>();

builder.Services.AddSingleton<IStockIntegrationEventMapper, StockIntegrationEventMapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for InventoryService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

// Start Kafka consumer in background
var consumer = app.Services.GetRequiredService<OrderEventConsumer>();
var cts = new CancellationTokenSource();

_ = Task.Run(async () =>
{
    Log.Information("Starting InventoryService Kafka consumer (listening to order.events)...");
    await consumer.StartConsumingAsync(cts.Token);
}, cts.Token);

app.UseHangfireDashboard("/hangfire");

// Register shutdown handler
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Stopping InventoryService consumer...");
    cts.Cancel();
});

app.Run();