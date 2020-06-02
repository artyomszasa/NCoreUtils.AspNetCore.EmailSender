using System.Text.Json;

namespace NCoreUtils
{
    public static class EmailSenderJsonOptions
    {
        public static JsonSerializerOptions Default { get; }

        static EmailSenderJsonOptions()
        {
            var defaultOptions = new JsonSerializerOptions();
            ApplyDefaults(defaultOptions);
            Default = defaultOptions;
        }

        public static void ApplyDefaults(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailAddress>());
            options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailAttachment>());
            options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailContent>());
            options.Converters.Add(ImmutableJsonConverterFactory.GetOrCreate<EmailMessage>());
        }
    }
}