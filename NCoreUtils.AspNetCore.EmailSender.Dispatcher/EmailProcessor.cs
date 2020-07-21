using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class EmailProcessor
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

        private readonly ILogger _logger;

        private readonly DispatcherConfig _config;

        public EmailProcessor(ILogger<EmailProcessor> logger, DispatcherConfig config)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public async Task ProcessRequestAsync(HttpContext context)
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
                _logger.LogError(exn, "Failed to deserializer pub/sub request message.");
                context.Response.StatusCode = 204; // Message should not be retried...
                return;
            }
            context.Response.StatusCode = await ProcessAsync(entry, req.Message.MessageId, context.RequestAborted);
            return;
        }

        public async Task<int> ProcessAsync(EmailMessageTask entry, string messageId, CancellationToken cancellationToken)
        {
            try
            {
                var dispatcher = _config.CreateSender(entry.Owner);
                var id = await dispatcher.ScheduleAsync(entry.Message, cancellationToken);
                _logger.LogInformation(
                    "Successfully sent email message [from = {0}, to = [{1}], cc = [{2}], bcc = [{3}], messageId = {4}] => {5}.",
                    entry.Message.From,
                    string.Join(", ", entry.Message.To),
                    string.Join(", ", entry.Message.Cc),
                    string.Join(", ", entry.Message.Bcc),
                    messageId,
                    id
                );
                return 202;
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to send email message [messageId = {0}].", messageId);
                return 200; // no retry...
            }
        }
    }
}