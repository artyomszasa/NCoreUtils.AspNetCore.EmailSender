using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

[method: JsonConstructor]
public class SendGridMessage(
    IReadOnlyList<Personalization> personalizations,
    EmailAddress from,
    string subject,
    IReadOnlyList<Content> content,
    EmailAddress? replyTo = default,
    IReadOnlyList<Attachment>? attachments = default)
{
    [JsonPropertyName("personalizations")]
    public IReadOnlyList<Personalization> Personalizations { get; } = personalizations;

    [JsonPropertyName("from")]
    public EmailAddress From { get; } = from;

    [JsonPropertyName("subject")]
    public string Subject { get; } = subject;

    [JsonPropertyName("content")]
    public IReadOnlyList<Content> Content { get; } = content;

    [JsonPropertyName("reply_to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public EmailAddress? ReplyTo { get; } = replyTo;

    [JsonPropertyName("attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<Attachment>? Attachments { get; } = attachments;

    public SendGridMessage(
        EmailAddress from,
        IReadOnlyList<EmailAddress> to,
        string subject,
        IReadOnlyList<Content> content,
        IReadOnlyList<EmailAddress>? cc,
        IReadOnlyList<EmailAddress>? bcc,
        IReadOnlyList<Attachment>? attachments)
        : this([new Personalization(to, cc, bcc)], from, subject, content, default, attachments)
    { }
}
