using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

public class EmailAddress(string email, string? name = null)
{
    [JsonPropertyName("email")]
    public string Email { get; } = email;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; } = name;
}
