using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher;

[JsonSerializable(typeof(PubSubRequest))]
internal partial class PubSubSerializerContext : JsonSerializerContext { }