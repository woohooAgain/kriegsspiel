using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Kriegsspiel.Core.Ai;

public static class AiStableId
{
    public static string Create(params object?[] parts)
    {
        var text = string.Join("\u001f", parts.Select(Part));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    public static int Int(params object?[] parts) =>
        unchecked((int)uint.Parse(Create(parts), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    private static string Part(object? value) => value switch
    {
        null => "<null>",
        IEnumerable<string> items => string.Join(",", items),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };
}
