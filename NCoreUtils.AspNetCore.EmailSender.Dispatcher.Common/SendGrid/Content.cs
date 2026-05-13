using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

public class Content(string type, string value)
{
    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("value")]
    public string Value { get; } = value;
}