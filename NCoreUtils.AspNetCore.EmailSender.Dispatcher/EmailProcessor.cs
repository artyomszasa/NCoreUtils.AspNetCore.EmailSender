using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public static class EmailProcessorExtensions
    {
        static private readonly JsonSerializerOptions requestSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static private readonly JsonSerializerOptions entrySerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                ImmutableJsonConverterFactory.GetOrCreate<EmailAddress>(),
                ImmutableJsonConverterFactory.GetOrCreate<EmailAttachment>(),
                ImmutableJsonConverterFactory.GetOrCreate<EmailContent>(),
                ImmutableJsonConverterFactory.GetOrCreate<EmailMessage>(),
                ImmutableJsonConverterFactory.GetOrCreate<EmailMessageTask>(),
            }
        };

        public static async Task ProcessRequestAsync(this EmailProcessor processor, HttpContext context)
        {
            PubSubRequest req;
            EmailMessageTask entry;
            try
            {
                req = await JsonSerializer.DeserializeAsync<PubSubRequest>(context.Request.Body, requestSerializerOptions, context.RequestAborted);
                entry = JsonSerializer.Deserialize<EmailMessageTask>(Convert.FromBase64String(req.Message.Data), entrySerializerOptions);
            }
            catch (Exception exn)
            {
                processor.Logger.LogError(exn, "Failed to deserializer pub/sub request message.");
                context.Response.StatusCode = 204; // Message should not be retried...
                return;
            }
            context.Response.StatusCode = await processor.ProcessAsync(entry, req.Message.MessageId, context.RequestAborted);
            return;
        }
    }
}