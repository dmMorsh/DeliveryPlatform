using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Utilities;

public static class DeterministicGuid
{
    public static Guid FromString(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    public static Guid FromComponents(params object?[] components)
    {
        if (components is null)
            throw new ArgumentNullException(nameof(components));

        var sb = new StringBuilder();
        for (var i = 0; i < components.Length; i++)
        {
            if (i > 0)
                sb.Append('|');
            sb.Append(Normalize(components[i]));
        }

        return FromString(sb.ToString());
    }

    private static string Normalize(object? component)
    {
        if (component is null)
            return string.Empty;

        return component switch
        {
            Guid g => g.ToString("N"),
            DateTime dt => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => component.ToString() ?? string.Empty
        };
    }
}