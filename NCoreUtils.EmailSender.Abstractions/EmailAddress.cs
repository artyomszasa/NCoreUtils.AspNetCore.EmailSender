using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils;

public class EmailAddress
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator EmailAddress(string email)
        => new(email, default);

    public string Email { get; }

    public string? Name { get; }

    public EmailAddress(string email, string? name = default)
    {
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Name = name;
    }

    // FIXME: noalloc version.
    public override string ToString()
        => string.IsNullOrEmpty(Name)
            ? Email
            : $"{Name} <{Email}>";
}