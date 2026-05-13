using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

public class Personalization(
    IReadOnlyList<EmailAddress> to,
    IReadOnlyList<EmailAddress>? cc = default,
    IReadOnlyList<EmailAddress>? bcc = default)
{
    [JsonPropertyName("to")]
    public IReadOnlyList<EmailAddress> To { get; } = to;

    [JsonPropertyName("cc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<EmailAddress>? Cc { get; } = cc;

    [JsonPropertyName("bcc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IReadOnlyList<EmailAddress>? Bcc { get; } = bcc;
}
