using System.Text.Json.Serialization;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid.Models;

public class EmailAddress(string email, string? name = null)
{
    [JsonPropertyName("email")]
    public string Email { get; } = email;

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; } = name;
}
