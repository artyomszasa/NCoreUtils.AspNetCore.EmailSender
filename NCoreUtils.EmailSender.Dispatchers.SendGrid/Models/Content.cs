using System.Text.Json.Serialization;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid.Models;

public class Content(string type, string value)
{
    [JsonPropertyName("type")]
    public string Type { get; } = type;

    [JsonPropertyName("value")]
    public string Value { get; } = value;
}