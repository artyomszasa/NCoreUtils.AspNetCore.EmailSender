namespace NCoreUtils;

public class EmailMessage
{
    public EmailAddress From { get; }

    public IReadOnlyList<EmailAddress> To { get; }

    public string Subject { get; }

    public IReadOnlyList<EmailAddress> Cc { get; }

    public IReadOnlyList<EmailAddress> Bcc { get; }

    public IReadOnlyList<EmailContent> Contents { get; }

    public IReadOnlyList<EmailAttachment> Attachments { get; }

    public EmailMessage(
        EmailAddress from,
        IReadOnlyList<EmailAddress> to,
        string subject,
        IReadOnlyList<EmailAddress> cc,
        IReadOnlyList<EmailAddress> bcc,
        IReadOnlyList<EmailContent> contents,
        IReadOnlyList<EmailAttachment> attachments)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException($"'{nameof(subject)}' cannot be null or empty.", nameof(subject));
        }

        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Subject = subject;
        Cc = cc ?? throw new ArgumentNullException(nameof(cc));
        Bcc = bcc ?? throw new ArgumentNullException(nameof(bcc));
        Contents = contents ?? throw new ArgumentNullException(nameof(contents));
        Attachments = attachments ?? throw new ArgumentNullException(nameof(attachments));
    }
}