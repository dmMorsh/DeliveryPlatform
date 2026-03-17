using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Shared.Services;

public static class ConfigurationGuard
{
    public static string GetRequired(IConfiguration config, IHostEnvironment env, string key, string? devDefault = null)
    {
        var value = config[key];
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (!env.IsProduction() && devDefault != null)
            return devDefault;

        throw new InvalidOperationException($"{key} configuration is required{(env.IsProduction() ? " in production." : ".")}");
    }

    public static string GetRequiredConnectionString(IConfiguration config, IHostEnvironment env, string name, string? devDefault = null)
        => GetRequired(config, env, $"ConnectionStrings:{name}", devDefault);

    public static string[] GetRequiredArray(IConfiguration config, IHostEnvironment env, string key, string[]? devDefault = null)
    {
        var value = config.GetSection(key).Get<string[]>();
        if (value is { Length: > 0 })
            return value;

        if (!env.IsProduction() && devDefault != null)
            return devDefault;

        throw new InvalidOperationException($"{key} configuration is required{(env.IsProduction() ? " in production." : ".")}");
    }
}
