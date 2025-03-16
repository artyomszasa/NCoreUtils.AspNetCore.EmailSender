using System.Text.Json.Serialization;

namespace NCoreUtils.Proto;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRootEmailSenderInfo))]
internal partial class EmailSenderSerializerContext : JsonSerializerContext { }