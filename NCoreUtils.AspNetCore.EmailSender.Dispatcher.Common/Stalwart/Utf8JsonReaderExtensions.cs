using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher.Stalwart;

internal static class Utf8JsonReaderExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Expect(this in Utf8JsonReader reader, JsonTokenType expected)
    {
        var actual = reader.TokenType;
        if (expected != actual)
        {
            throw new JsonException($"Expected {expected} found {actual}.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadOrThrow(this ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON stream.");
        }
    }

    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
        => options.GetTypeInfo(typeof(T)) switch
        {
            null => throw new InvalidOperationException($"No type info found for {typeof(T).Name}."),
            JsonTypeInfo<T> typeInfo => typeInfo,
            _ => throw new InvalidOperationException($"Invalid type info found for {typeof(T).Name}.")
        };
}
