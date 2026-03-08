using System.Threading.RateLimiting;
using Confluent.Kafka;
using MediatR;
using PaymentService.Api.Grpc;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Services;
using PaymentService.Api.Security;
using PaymentService.Infrastructure.Jobs;
using PaymentService.Infrastructure.Mapping;
using PaymentService.Infrastructure.Inbox;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Outbox;
using PaymentService.Infrastructure.Providers;
using PaymentService.Infrastructure.Sharding;
using PaymentService.Application.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.MediatR;
using Serilog;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceTelemetry("payment-service");

builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .WriteTo.OpenTelemetry()
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("../../logs/PaymentService-.log",
           rollingInterval: RollingInterval.Day,
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

var postgresConnectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));
builder.Services.AddDbContext<PaymentShardMapDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var httpTimeoutSeconds = int.TryParse(builder.Configuration["Http:TimeoutSeconds"], out var httpTimeout)
    ? httpTimeout
    : 10;

var catalogHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");

var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "redis:6379");

try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    redisOptions.AbortOnConnectFail = false;
    var mux = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    catalogHealthChecks.AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
}

builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddSingleton<IPaymentStatusCache, PaymentStatusRedisCache>();
builder.Services.AddSingleton<IPaymentDbContextFactory, PaymentDbContextFactory>();
builder.Services.Configure<PaymentShardOptions>(builder.Configuration.GetSection("Payments:Sharding"));
builder.Services.AddSingleton<IPaymentShardRouter, PaymentShardRouter>();
builder.Services.Configure<PaymentShardMapOptions>(builder.Configuration.GetSection("Payments:ShardMap"));
builder.Services.AddSingleton<IPaymentShardMapDbContextFactory, PaymentShardMapDbContextFactory>();
builder.Services.Configure<SberbankOptions>(builder.Configuration.GetSection("Payments:Sberbank"));
builder.Services.Configure<YooMoneyOptions>(builder.Configuration.GetSection("Payments:YooMoney"));
builder.Services.Configure<FakePaymentOptions>(builder.Configuration.GetSection("Payments:FakeProvider"));
builder.Services.Configure<PaymentStatusCheckOptions>(builder.Configuration.GetSection("Payments:StatusCheck"));
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection("Payments:Webhooks"));
builder.Services.AddHttpClient<SberbankPaymentProvider>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(
            sp.GetRequiredService<ILogger<SberbankPaymentProvider>>(),
            httpTimeoutSeconds));
builder.Services.AddHttpClient<YooMoneyPaymentProvider>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(
            sp.GetRequiredService<ILogger<YooMoneyPaymentProvider>>(),
            httpTimeoutSeconds));
builder.Services.AddHttpClient<FakePaymentProvider>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(
            sp.GetRequiredService<ILogger<FakePaymentProvider>>(),
            httpTimeoutSeconds));
builder.Services.AddScoped<IPaymentProvider, SberbankPaymentProvider>();
builder.Services.AddScoped<IPaymentProvider, YooMoneyPaymentProvider>();
builder.Services.AddScoped<IPaymentProvider, FakePaymentProvider>();
builder.Services.AddScoped<IPaymentProviderResolver, PaymentProviderResolver>();
builder.Services.AddScoped<IPaymentStatusCheckScheduler, PaymentStatusCheckScheduler>();
builder.Services.AddSingleton<IWebhookValidator, WebhookValidator>();
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<OutboxCleanupHostedService<PaymentDbContext, OutboxMessage>>();
builder.Services.AddSingleton<IPaymentIntegrationEventMapper, PaymentIntegrationEventMapper>();
builder.Services.AddSingleton<PaymentEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<PaymentEventConsumer>>();
builder.Services.AddScoped<IEventInbox, PaymentEventInbox>();
builder.Services.AddHostedService<ProcessedEventCleanupHostedService<PaymentDbContext>>();

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("payment-default", httpContext =>
    {
        var key = httpContext.User?.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity!.Name ?? httpContext.User.FindFirst("sub")?.Value ?? "unknown"}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 60,
            TokensPerPeriod = 60,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

builder.Services.AddHangfire(config =>
{
    config.UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    var connectionString = builder.Configuration.GetConnectionString("Hangfire");
    if (!string.IsNullOrWhiteSpace(connectionString))
        config.UsePostgreSqlStorage(connectionString);
    else
    {
        if (builder.Environment.IsProduction())
            throw new InvalidOperationException("Hangfire connection string is required in production.");
        config.UseMemoryStorage();
    }
});
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsProduction())
{
    var sberbank = builder.Configuration.GetSection("Payments:Sberbank").Get<SberbankOptions>();
    var yoomoney = builder.Configuration.GetSection("Payments:YooMoney").Get<YooMoneyOptions>();

    var hasSberbank = sberbank is not null
                      && !string.IsNullOrWhiteSpace(sberbank.BaseUrl)
                      && !string.IsNullOrWhiteSpace(sberbank.UserName)
                      && !string.IsNullOrWhiteSpace(sberbank.Password)
                      && !string.IsNullOrWhiteSpace(sberbank.ReturnUrl)
                      && !string.IsNullOrWhiteSpace(sberbank.FailUrl);

    var hasYooMoney = yoomoney is not null
                      && !string.IsNullOrWhiteSpace(yoomoney.BaseUrl)
                      && !string.IsNullOrWhiteSpace(yoomoney.SecretKey)
                      && !string.IsNullOrWhiteSpace(yoomoney.ShopId)
                      && !string.IsNullOrWhiteSpace(yoomoney.ReturnUrl)
                      && !string.IsNullOrWhiteSpace(yoomoney.FailUrl);

    if (!hasSberbank && !hasYooMoney)
        throw new InvalidOperationException("At least one payment provider must be configured in production.");
}

app.MapControllers().RequireRateLimiting("payment-default");
app.MapGrpcService<PaymentGrpcService>();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
dbContext.Database.Migrate();
var mdbContext = scope.ServiceProvider.GetRequiredService<PaymentShardMapDbContext>();
mdbContext.Database.Migrate();
Log.Information("Database migration completed for PaymentService");

app.Run();
