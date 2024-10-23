namespace PlumbBuddy;

static partial class Extensions
{
    [GeneratedRegex(@"[^\da-f]", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GetNonHexStringCharacterPattern();

    public static IEnumerable<byte> ToByteSequence(this string hex)
    {
        if (!TryToByteSequence(hex, out var sequence))
            throw new ArgumentException("not a valid hex string", nameof(hex));
        return sequence;
    }

    public static bool TryToByteSequence(this string hex, [NotNullWhen(true)] out IEnumerable<byte>? sequence)
    {
        if (hex is null
            || hex.Length % 2 != 0
            || GetNonHexStringCharacterPattern().IsMatch(hex))
        {
            sequence = default;
            return false;
        }
        sequence = Enumerable
            .Range(0, hex.Length / 2)
            .Select(byteIndex => hex.Substring(byteIndex * 2, 2))
            .Select(byteHex => byte.Parse(byteHex, NumberStyles.HexNumber));
        return true;
    }

    public static string ToHexString(this IEnumerable<byte> bytes) =>
        string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));
}
