using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

internal sealed class MaybeStringConverter : JsonConverter<Maybe<string?>>
{
    public override Maybe<string?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => new(null),
            JsonTokenType.String => new(reader.GetString()),
            var tokenType => throw new JsonException($"Cannot convert JSON sequence starting with {tokenType} to Maybe<string>")
        };
    }

    public override void Write(Utf8JsonWriter writer, Maybe<string?> value, JsonSerializerOptions options)
    {
        if (value.TryGetValue(out var s) && s is not null)
        {
            writer.WriteStringValue(s);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(MaybeStringConverter)]
)]
[JsonSerializable(typeof(BlobUploadResponse))]
[JsonSerializable(typeof(JmapCall))]
[JsonSerializable(typeof(JmapResponse))]
[JsonSerializable(typeof(EmailSetArguments))]
[JsonSerializable(typeof(EmailSubmissionSetArguments))]
[JsonSerializable(typeof(MailboxQueryArguments))]
[JsonSerializable(typeof(IdentityGetResponse))]
[JsonSerializable(typeof(MailboxQueryResult))]
[JsonSerializable(typeof(EmailSetResult))]
[JsonSerializable(typeof(EmailSubmissionResult))]
internal sealed partial class StalwartSerializerContext : JsonSerializerContext { }