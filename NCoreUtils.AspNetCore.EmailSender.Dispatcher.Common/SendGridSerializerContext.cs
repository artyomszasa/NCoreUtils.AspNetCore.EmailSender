using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

[JsonSerializable(typeof(SendGridErrorResponse))]
internal partial class SendGridSerializerContext : JsonSerializerContext { }