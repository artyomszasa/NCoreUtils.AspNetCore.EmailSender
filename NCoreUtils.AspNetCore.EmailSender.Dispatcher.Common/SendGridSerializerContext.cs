using System.Text.Json.Serialization;
using NCoreUtils.AspNetCore.EmailSender.Dispatcher.SendGrid;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

[JsonSerializable(typeof(SendGridMessage))]
[JsonSerializable(typeof(SendGridErrorResponse))]
internal partial class SendGridSerializerContext : JsonSerializerContext { }