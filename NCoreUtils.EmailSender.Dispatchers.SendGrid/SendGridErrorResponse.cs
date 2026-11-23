using System.Text.Json.Serialization;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid;

public class SendGridErrorResponse(IReadOnlyList<SendGridError> errors)
{
    [JsonPropertyName("errors")]
    public IReadOnlyList<SendGridError> Errors { get; } = errors;
}