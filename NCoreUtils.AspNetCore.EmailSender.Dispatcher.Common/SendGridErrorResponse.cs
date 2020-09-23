using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class SendGridErrorResponse
    {
        [JsonPropertyName("errors")]
        public List<SendGridError> Errors { get; set; } = new List<SendGridError>();
    }
}