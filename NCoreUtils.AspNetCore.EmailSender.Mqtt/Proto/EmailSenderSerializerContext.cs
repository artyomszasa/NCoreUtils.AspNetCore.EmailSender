using System.Text.Json.Serialization;
using NCoreUtils.Proto;

namespace NCoreUtils.AspNetCore.Proto;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonRootEmailSenderInfo))]
internal partial class EmailSenderSerializerContext : JsonSerializerContext { }