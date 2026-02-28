using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using LocationHistoryExporter.Services;
using Nest;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cb) => cb.AddEnvironmentVariables())
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;
        var redisConn = cfg["Redis__Connection"] ?? "localhost:6379";
        var mux = ConnectionMultiplexer.Connect(redisConn);
        services.AddSingleton<IConnectionMultiplexer>(mux);
        services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

        var esUri = cfg["ELASTIC__URI"] ?? "http://localhost:9200";
        var indexName = cfg["EXPORTER__INDEX"] ?? "courier-history";
        var settings = new ConnectionSettings(new Uri(esUri)).DefaultIndex(indexName);
        var es = new ElasticClient(settings);
        services.AddSingleton<IElasticClient>(es);

        services.Configure<ExporterOptions>(opts =>
        {
            opts.IntervalSeconds = int.TryParse(cfg["EXPORTER__INTERVAL_SECONDS"], out var v) ? v : 60;
            opts.HistoryKeyPattern = cfg["EXPORTER__HISTORY_PATTERN"] ?? "courier:*:history";
            opts.IndexName = indexName;
        });

        services.AddHostedService<HistoryExporter>();
    })
    .ConfigureLogging(l => l.AddConsole())
    .Build();

await host.RunAsync();
