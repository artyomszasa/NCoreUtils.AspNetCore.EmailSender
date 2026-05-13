using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

public class Attachment(
    byte[] content,
    string filename,
    string? type = default,
    string? disposition = default,
    string? contentId = default)
{
    [JsonPropertyName("content")]
    public byte[] Content { get; } = content;

    [JsonPropertyName("filename")]
    public string Filename { get; } = filename;

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Type { get; } = type;

    [JsonPropertyName("disposition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Disposition { get; } = disposition;

    [JsonPropertyName("content_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ContentId { get; } = contentId;
}
