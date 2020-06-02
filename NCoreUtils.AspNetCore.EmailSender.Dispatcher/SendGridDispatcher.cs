using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class SendGridDispatcher : IEmailSender
    {
        protected SendGridClientOptions ClientOptions { get; }

        protected ILogger Logger { get; }

        protected IHttpClientFactory? HttpClientFactory { get; }

        public SendGridDispatcher(
            SendGridClientOptions clientOptions,
            ILogger<SendGridDispatcher> logger,
            IHttpClientFactory? httpClientFactory)
        {
            ClientOptions = clientOptions;
            Logger = logger;
            HttpClientFactory = httpClientFactory;
        }

        protected HttpClient CreateClient()
            => HttpClientFactory?.CreateClient(nameof(SendGridDispatcher)) ?? new HttpClient();

        public async Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            using var httpClient = CreateClient();
            var sendGrid = new SendGridClient(httpClient, ClientOptions);
            var m = new SendGridMessage();
            m.SetFrom(message.From.ToSendGridAddress());
            m.SetSubject(message.Subject);
            m.AddTos(message.To.Select(SendGridExtensions.ToSendGridAddress).ToList());
            m.AddCcs(message.Cc.Select(SendGridExtensions.ToSendGridAddress).ToList());
            m.AddBccs(message.Bcc.Select(SendGridExtensions.ToSendGridAddress).ToList());
            foreach (var content in message.Contents)
            {
                m.AddContent(content.MediaType, content.Content);
            }
            foreach (var attachment in message.Attachments)
            {
                m.AddAttachment(attachment.Filename, Convert.ToBase64String(attachment.Data), attachment.MediaType, attachment.Disposition.ToSendGridDisposition(),  attachment.ContentId);
            }
            var response = await sendGrid.SendEmailAsync(m, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return string.Empty;
            }
            if (response.StatusCode == HttpStatusCode.Created)
            {
                return string.Empty;
            }
            using var stream = await response.Body.ReadAsStreamAsync();
            var errorResponse = await JsonSerializer.DeserializeAsync<SendGridErrorResponse>(stream);
            if (errorResponse.Errors.Count == 0)
            {
                Logger.LogError("Sending mail has failed without an error.");
                return string.Empty; // no retry
            }
            foreach (var error in errorResponse.Errors)
            {
                Logger.LogError($"SendGrid delivery error: [{error.Field} {error.ErrorId}] {error.Message}");
            }
            return string.Empty;
        }
    }
}