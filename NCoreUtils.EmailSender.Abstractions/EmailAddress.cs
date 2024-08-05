using System.Runtime.CompilerServices;

namespace NCoreUtils;

public class EmailAddress(string email, string? name = default)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator EmailAddress(string email)
        => new(email, default);

    public string Email { get; } = email ?? throw new ArgumentNullException(nameof(email));

    public string? Name { get; } = name;

    // FIXME: noalloc version.
    public override string ToString()
        => string.IsNullOrEmpty(Name)
            ? Email
            : $"{Name} <{Email}>";
}