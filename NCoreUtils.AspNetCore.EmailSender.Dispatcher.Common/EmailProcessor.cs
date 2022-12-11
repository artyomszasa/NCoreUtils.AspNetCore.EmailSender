using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Internal;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class EmailProcessor
    {
        private readonly DispatcherConfig _config;

        public ILogger Logger { get; }

        public EmailProcessor(ILogger<EmailProcessor> logger, DispatcherConfig config)
        {
            Logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public async Task<int> ProcessAsync(EmailMessageTask entry, string messageId, CancellationToken cancellationToken)
        {
            try
            {
                var dispatcher = _config.CreateSender(entry.Owner);
                var id = await dispatcher.ScheduleAsync(entry.Message, cancellationToken);
                Logger.LogInformation(
                    "Successfully sent email message [from = {Sender}, to = [{Recipients}], cc = [{Cc}], bcc = [{Bcc}], messageId = {MessageId}] => {Id}.",
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
                Logger.LogError(exn, "Failed to send email message [messageId = {MessageId}].", messageId);
                return 200; // no retry...
            }
        }
    }
}