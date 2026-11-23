using System.Text.Json.Serialization;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid;

public class SendGridError(string message, string? field)
{
    [JsonPropertyName("message")]
    public string Message { get; } = message;

    [JsonPropertyName("field")]
    public string? Field { get; } = field;
}