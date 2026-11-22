namespace NCoreUtils;

public struct EmailMessageBuilder
{
    public EmailAddress? From { get; set; }

    public ListWrapper<EmailAddress> To;

    public string? Subject { get; set; }

    public string? ReplyTo { get; set; }

    public ListWrapper<EmailAddress> Cc;

    public ListWrapper<EmailAddress> Bcc;

    public ListWrapper<EmailContent> Contents;

    public ListWrapper<EmailAttachment> Attachments;

    public ListWrapper<EmailListUnsubscribeOption> OneClickUnsubscribe;

    public readonly EmailMessage Build() => new(
        From!,
        To.ToArray(),
        Subject!,
        ReplyTo,
        Cc.ToArray(),
        Bcc.ToArray(),
        Contents.ToArray(),
        Attachments.ToArray(),
        OneClickUnsubscribe.Empty ? default : OneClickUnsubscribe.ToArray()
    );
}