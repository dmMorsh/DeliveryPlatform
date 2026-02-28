namespace OrderService.Application.Services;

public static class KitchenPauseState
{
    private static readonly object Gate = new();
    private static DateTime? _pausedUntil;

    public static bool TryGetPausedUntil(out DateTime? pausedUntil)
    {
        lock (Gate)
        {
            if (_pausedUntil.HasValue && _pausedUntil.Value <= DateTime.UtcNow)
                _pausedUntil = null;

            pausedUntil = _pausedUntil;
            return _pausedUntil.HasValue;
        }
    }

    public static void PauseUntil(DateTime untilUtc)
    {
        lock (Gate)
        {
            if (!_pausedUntil.HasValue || untilUtc > _pausedUntil.Value)
                _pausedUntil = untilUtc;
        }
    }
}
