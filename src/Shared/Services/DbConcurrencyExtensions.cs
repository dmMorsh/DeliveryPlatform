using Microsoft.EntityFrameworkCore;

namespace Shared.Services;

public static class DbConcurrencyExtensions
{
    public static async Task SaveChangesWithConcurrencyRetryAsync(
        this DbContext db,
        int maxRetries,
        CancellationToken ct = default)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));

        var attempt = 0;
        while (true)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
            {
                attempt++;
                foreach (var entry in ex.Entries)
                {
                    var dbValues = await entry.GetDatabaseValuesAsync(ct);
                    if (dbValues == null)
                        throw;

                    // Last-write-wins: keep current values, refresh original RowVersion.
                    entry.OriginalValues.SetValues(dbValues);
                }

                var backoffMs = Math.Min(500, 50 * attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(backoffMs), ct);
            }
        }
    }
}
