using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NCoreUtils.Internal;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class EmailProcessor(ILogger<EmailProcessor> logger, DispatcherConfig config)
    {
        private readonly DispatcherConfig _config = Preconditions.ThrowIfNull(config);

        private ConcurrentDictionary<string, IEmailSender> SenderCache { get; } = new();

        public ILogger Logger { get; } = Preconditions.ThrowIfNull(logger);

        public async Task<int> ProcessAsync(EmailMessageTask entry, string messageId, CancellationToken cancellationToken)
        {
            try
            {
                var dispatcher = SenderCache.GetOrAdd(entry.Owner, _config.CreateSender);
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