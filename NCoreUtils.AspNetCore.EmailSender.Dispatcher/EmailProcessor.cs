using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Internal;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public static class EmailProcessorExtensions
    {
        public static async Task ProcessRequestAsync(this EmailProcessor processor, HttpContext context)
        {
            PubSubRequest req;
            EmailMessageTask entry;
            try
            {
                req = (await JsonSerializer
                    .DeserializeAsync(context.Request.Body, PubSubSerializerContext.Default.PubSubRequest, context.RequestAborted)
                    .ConfigureAwait(false))
                    ?? throw new InvalidOperationException("Could not deserialize Pub/Sub request.");
                entry = JsonSerializer
                    .Deserialize(Convert.FromBase64String(req.Message.Data), EmailMessageTaskSerializerContext.Default.EmailMessageTask)
                    ?? throw new InvalidOperationException("Could not deserialize email message task.");
            }
            catch (Exception exn)
            {
                processor.Logger.LogError(exn, "Failed to deserializer pub/sub request message.");
                context.Response.StatusCode = 204; // Message should not be retried...
                return;
            }
            context.Response.StatusCode = await processor
                .ProcessAsync(entry, req.Message.MessageId, context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }
    }
}