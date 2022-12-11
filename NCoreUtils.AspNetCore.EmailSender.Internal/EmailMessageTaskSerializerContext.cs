using System.Text.Json.Serialization;

namespace NCoreUtils.Internal;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EmailMessageTask))]
public partial class EmailMessageTaskSerializerContext : JsonSerializerContext { }