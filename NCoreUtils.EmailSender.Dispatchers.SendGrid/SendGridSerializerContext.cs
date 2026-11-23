using System.Text.Json.Serialization;
using NCoreUtils.EmailSender.Dispatchers.SendGrid.Models;

namespace NCoreUtils.EmailSender.Dispatchers.SendGrid;

[JsonSerializable(typeof(SendGridMessage))]
[JsonSerializable(typeof(SendGridErrorResponse))]
internal partial class SendGridSerializerContext : JsonSerializerContext { }