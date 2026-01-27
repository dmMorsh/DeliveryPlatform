using System.Text;
using CartService.Application;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Application.Services;
using CartService.Infrastructure;
using CartService.Infrastructure.Outbox;
using CartService.Infrastructure.Persistence;
using CartService.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Proto;
using Shared.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory)
{
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
                           ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
{
    o.Address = new Uri("http://localhost:5001");
});
// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();

// Cart DDD services
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartReadRepository, CartReadRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ICartIntegrationEventMapper, CartEventMapper>();
// Event Consumer from OrderService
builder.Services.AddSingleton<OrderEventConsumer>();
// Outbox processor
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

// Auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUPER_PUPER_SECRET_KEY_I_LOVE_MAKING_KEYS_UP";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "identity-service";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "platform-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Headers["authorization"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken.ToString().Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

//Auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "OK");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CartService");
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
    Log.Information("Starting CartService Kafka consumer (listening to order.events)...");
    await consumer.StartConsumingAsync(cts.Token);
}, cts.Token);

// Register shutdown handler
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Stopping CartService consumer...");
    cts.Cancel();
});

app.Run();