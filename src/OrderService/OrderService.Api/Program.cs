using System.Text;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Mapping;
using OrderService.Api.Mappings;
using OrderService.Api.Grpc;
using Shared.Services;
using MediatR;
using OrderService.Application;
using OrderService.Application.Services;
using OrderService.Infrastructure.Outbox;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .MinimumLevel.Information()
        .Filter.ByExcluding(le =>
            le.Level == LogEventLevel.Information 
            && le.Properties.TryGetValue("commandText", out var cmd)
            && cmd.ToString().StartsWith("\"-- OUTBOX_PROCESSOR_POLL"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/orderservice-YYYY-MM-DD.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    );

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory)
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
        ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddSingleton<IOrderIntegrationEventMapper, IntegrationEventMapper>();
// Event Consumer from other services
builder.Services.AddSingleton<OrderEventConsumer>();

// builder.Services.AddScoped<OrderApplicationService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton<KafkaEventProducer>();
// Only run OutboxProcessor when using a real relational DB
if (!useInMemory) 
    builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Register MediatR handlers from Application assembly
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));;

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
var connectionString1 = builder.Configuration.GetConnectionString("PostgreSQL") 
                       ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString1)
    .AddKafka(new ProducerConfig
        {
            BootstrapServers = "kafka:9092"
        },
        name: "kafka")
    ;

var app = builder.Build();

MapsterConfig.RegisterMappings();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<OrderGrpcService>();
app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for OrderService");
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
    Log.Information("Starting OrderService Kafka consumer (listening to cart.events, courier.events)...");
    await consumer.StartConsumingAsync(cts.Token);
}, cts.Token);

// Register shutdown handler
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Stopping OrderService consumer...");
    cts.Cancel();
});

app.Run();
