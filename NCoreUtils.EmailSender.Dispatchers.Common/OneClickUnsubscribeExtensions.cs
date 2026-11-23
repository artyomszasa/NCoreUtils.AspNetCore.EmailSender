using System.Globalization;

namespace NCoreUtils.EmailSender.Dispatchers;

public static class OneClickUnsubscribeExtensions
{
    private static ReadOnlySpan<char> Separator => ", ";

    private static bool TryFormatHeaderValue(IReadOnlyList<EmailListUnsubscribeOption> unsubscribeOptions, Span<char> buffer, out int used)
    {
        var offset = 0;
        foreach (var item in unsubscribeOptions)
        {
            if (offset != 0)
            {
                if (!Separator.TryCopyTo(buffer[offset..]))
                {
                    used = 0;
                    return false;
                }
                offset += Separator.Length;
            }
            if (!item.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
            {
                used = 0;
                return false;
            }
            offset += written;
        }
        used = offset;
        return true;
    }

    public static string ToHeaderValue(this IReadOnlyList<EmailListUnsubscribeOption> unsubscribeOptions)
    {
        Preconditions.ThrowIfNull(unsubscribeOptions);
        Span<char> buffer = stackalloc char[512];
        if (TryFormatHeaderValue(unsubscribeOptions, buffer, out var written))
        {
            return new(buffer[..written]);
        }
        return string.Join(", ", unsubscribeOptions);
    }
}