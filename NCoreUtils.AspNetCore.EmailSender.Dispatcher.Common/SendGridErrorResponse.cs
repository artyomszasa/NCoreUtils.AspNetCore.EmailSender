using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

public class SendGridErrorResponse(IReadOnlyList<SendGridError> errors)
{
    [JsonPropertyName("errors")]
    public IReadOnlyList<SendGridError> Errors { get; } = errors;
}