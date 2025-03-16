using System.Globalization;
using System.Runtime.CompilerServices;

namespace NCoreUtils;

public class EmailAddress(string email, string? name = default)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator EmailAddress(string email)
        => new(email, default);

    public string Email { get; } = email ?? throw new ArgumentNullException(nameof(email));

    public string? Name { get; } = name;

#if NET6_0_OR_GREATER

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return Email;
        }
        if (Email.Length + Name.Length + 3 < 512)
        {
            Span<char> buffer = stackalloc char[512];
            return string.Create(CultureInfo.InvariantCulture, buffer, $"{Name} <{Email}>");
        }
        return $"{Name} <{Email}>";
    }

#else

    public override string ToString()
        => string.IsNullOrEmpty(Name)
            ? Email
            : $"{Name} <{Email}>";

#endif

}