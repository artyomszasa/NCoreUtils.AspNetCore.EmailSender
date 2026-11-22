using System.Text.Json.Serialization;

namespace NCoreUtils;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EmailListUnsubscribeUrl))]
public abstract class EmailListUnsubscribeOption
#if NET6_0_OR_GREATER
    : ISpanFormattable
#else
    : IFormattable
#endif
{
    public abstract string ToString(string? format, IFormatProvider? formatProvider);

    public abstract bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider);

    public override string ToString()
        => ToString(default, default);
}

public sealed class EmailListUnsubscribeUrl(string url)
    : EmailListUnsubscribeOption
{
    public string Url { get; } = url;

    public override string ToString(string? format, IFormatProvider? formatProvider)
        => $"<{Url}>";

    public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < Url.Length + 2)
        {
            charsWritten = 0;
            return false;
        }
        destination[0] = '<';
        Url.AsSpan().CopyTo(destination[1..]);
        destination[Url.Length + 1] = '>';
        charsWritten = Url.Length + 2;
        return true;
    }
}

public sealed class EmailListUnsubscribeEmail(string email)
    : EmailListUnsubscribeOption
{
    private static ReadOnlySpan<char> Prefix => "<mailto:";

    public string Email { get; } = email;

    public override string ToString(string? format, IFormatProvider? formatProvider)
        => $"<mailto:{Email}>";

    public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < Email.Length + 9)
        {
            charsWritten = 0;
            return false;
        }
        Prefix.CopyTo(destination);
        Email.AsSpan().CopyTo(destination[8..]);
        destination[Email.Length + 8] = '>';
        charsWritten = Email.Length + 9;
        return true;
    }
}

[method: JsonConstructor]
public class EmailMessage(
    EmailAddress from,
    IReadOnlyList<EmailAddress> to,
    string subject,
    string? replyTo,
    IReadOnlyList<EmailAddress> cc,
    IReadOnlyList<EmailAddress> bcc,
    IReadOnlyList<EmailContent> contents,
    IReadOnlyList<EmailAttachment> attachments,
    IReadOnlyList<EmailListUnsubscribeOption>? oneClickUnsubscribe = default)
{
    public EmailAddress From { get; } = Preconditions.ThrowIfNull(from);

    public IReadOnlyList<EmailAddress> To { get; } = Preconditions.ThrowIfNull(to);

    public string Subject { get; } = Preconditions.ThrowIfNullOrEmpty(subject);

    public string? ReplyTo { get; } = replyTo;

    public IReadOnlyList<EmailAddress> Cc { get; } = Preconditions.ThrowIfNull(cc);

    public IReadOnlyList<EmailAddress> Bcc { get; } = Preconditions.ThrowIfNull(bcc);

    public IReadOnlyList<EmailContent> Contents { get; } = Preconditions.ThrowIfNull(contents);

    public IReadOnlyList<EmailAttachment> Attachments { get; } = Preconditions.ThrowIfNull(attachments);

    public IReadOnlyList<EmailListUnsubscribeOption>? OneClickUnsubscribe { get; } = oneClickUnsubscribe;

    public EmailMessage(
        EmailAddress from,
        IReadOnlyList<EmailAddress> to,
        string subject,
        IReadOnlyList<EmailAddress> cc,
        IReadOnlyList<EmailAddress> bcc,
        IReadOnlyList<EmailContent> contents,
        IReadOnlyList<EmailAttachment> attachments)
        : this(from, to, subject, default, cc, bcc, contents, attachments, default)
    { }
}