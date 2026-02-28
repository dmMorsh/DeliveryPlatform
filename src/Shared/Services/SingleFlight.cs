using System.Collections.Concurrent;

namespace Shared.Services;

public sealed class SingleFlight<TKey, TResult> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TResult>>> _inflight = new();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public Task<TResult> RunAsync(TKey key, Func<Task<TResult>> factory)
        => RunAsync(key, _ => factory(), DefaultTimeout);

    public Task<TResult> RunAsync(TKey key, Func<CancellationToken, Task<TResult>> factory, TimeSpan? timeout = null)
    {
        var lazy = _inflight.GetOrAdd(
            key,
            k => new Lazy<Task<TResult>>(
                () => RunAndCleanupAsync(k, factory, timeout ?? DefaultTimeout),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazy.Value;
    }

    private async Task<TResult> RunAndCleanupAsync(TKey key, Func<CancellationToken, Task<TResult>> factory, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            return await factory(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }
}
