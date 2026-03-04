using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Shared.Services;

/// <summary>
/// Distributed rate limiter with adaptive throttling based on downstream health.
/// Stores permit state in Redis for multi-instance coordination.
/// </summary>
public interface IDistributedRateLimiter
{
    Task<bool> TryAcquireAsync();
    void SetPermitLimit(int limit);
    int GetCurrentPermitLimit();
    int GetAvailablePermits();
}

public class DistributedRateLimiter : IDistributedRateLimiter
{
    private readonly IDatabase _db;
    private readonly ILogger<DistributedRateLimiter> _logger;
    private readonly string _rateLimitKey;
    private int _currentPermitLimit;
    private DateTime _windowStart = DateTime.UtcNow;
    private const int DefaultPermitLimit = 1000;
    private const int WindowDurationSeconds = 1;

    public DistributedRateLimiter(IConnectionMultiplexer redis, ILogger<DistributedRateLimiter> logger, string? keyPrefix = null)
    {
        _db = redis.GetDatabase();
        _logger = logger;
        _rateLimitKey = $"{keyPrefix ?? "ratelimit"}:permits:{DateTime.UtcNow:yyyyMMdd}";
        _currentPermitLimit = DefaultPermitLimit;
    }

    public async Task<bool> TryAcquireAsync()
    {
        var now = DateTime.UtcNow;
        
        // Reset window if expired
        if ((now - _windowStart).TotalSeconds >= WindowDurationSeconds)
        {
            _windowStart = now;
            _rateLimitKey.Replace(DateTime.UtcNow.ToString("yyyyMMdd"), DateTime.UtcNow.ToString("yyyyMMdd"));
        }

        try
        {
            // Redis INCR is atomic; if result > limit, reject
            var currentCount = await _db.StringIncrementAsync(_rateLimitKey);
            
            if (currentCount == 1)
            {
                // First request in window, set expiry
                await _db.KeyExpireAsync(_rateLimitKey, TimeSpan.FromSeconds(WindowDurationSeconds * 2));
            }

            if (currentCount <= _currentPermitLimit)
            {
                return true;
            }

            _logger.LogWarning("Rate limit exceeded: {Count}/{Limit}", currentCount, _currentPermitLimit);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit; allowing request");
            return true; // Fail open – let the request through if Redis is down
        }
    }

    public void SetPermitLimit(int limit)
    {
        _currentPermitLimit = limit;
        _logger.LogInformation("Rate limit adjusted to {Limit} permits/sec", limit);
    }

    public int GetCurrentPermitLimit() => _currentPermitLimit;

    public int GetAvailablePermits()
    {
        try
        {
            var count = (long)_db.StringGet(_rateLimitKey);
            return Math.Max(0, _currentPermitLimit - (int)count);
        }
        catch
        {
            return _currentPermitLimit;
        }
    }
}
