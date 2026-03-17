using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace Shared.Services;

public static class SerilogExtensions
{
    public static void UseExtededSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) =>
            cfg
                .MinimumLevel.Information()
                .Filter.ByExcluding(le =>
                    le.Level == LogEventLevel.Information
                    && le.Properties.TryGetValue("commandText", out var cmd)
                    && cmd.ToString().StartsWith("\"-- INFRA_BACKGROUND_POLL"))
                .WriteTo.OpenTelemetry()
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("../../logs/DeliveryService-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        );
    }
}