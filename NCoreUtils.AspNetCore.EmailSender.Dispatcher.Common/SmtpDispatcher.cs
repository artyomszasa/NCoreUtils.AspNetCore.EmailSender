using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public class SmtpDispatcher : IEmailSender
    {
        private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false);

        private readonly SmtpConfiguration _configuration;

        public SmtpDispatcher(SmtpConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> ScheduleAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            var email = new MimeMessage();
            email.From.Add(message.From.ToMailboxAddress());
            email.Subject = message.Subject;
            if (message.To.Count > 0)
            {
                email.To.AddRange(message.To.Select(SmtpExtensions.ToMailboxAddress));
            }
            if (message.Cc.Count > 0)
            {
                email.Cc.AddRange(message.Cc.Select(SmtpExtensions.ToMailboxAddress));
            }
            if (message.Bcc.Count > 0)
            {
                email.Bcc.AddRange(message.Bcc.Select(SmtpExtensions.ToMailboxAddress));
            }
            var bodyBuilder = new BodyBuilder();
            foreach (var content in message.Contents)
            {
                if (content.MediaType == "text/html")
                {
                    bodyBuilder.HtmlBody = content.Content;
                }
                else if (content.MediaType == "text/plain")
                {
                    bodyBuilder.TextBody = content.Content;
                }
                else
                {
                    // FIXME: handle custom contents...
                }
            }
            foreach (var attachment in message.Attachments)
            {
                var img = bodyBuilder.LinkedResources.Add(attachment.Filename, attachment.Data, ContentType.Parse(attachment.MediaType));
                img.ContentId = attachment.ContentId;
                img.ContentDisposition = attachment.Disposition switch
                {
                    EmailAttachmentDisposition.Attachment => new ContentDisposition(ContentDisposition.Attachment),
                    EmailAttachmentDisposition.Inline => new ContentDisposition(ContentDisposition.Inline),
                    _ => new ContentDisposition()
                };
            }
            email.Body = bodyBuilder.ToMessageBody();
            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration.Host, _configuration.Port, _configuration.UseSsl, cancellationToken);
            if (_configuration.User.HasValue)
            {
                await client.AuthenticateAsync(_utf8, _configuration.User.Value.Username, _configuration.User.Value.Password, cancellationToken);
            }
            await client.SendAsync(email, cancellationToken);
            await client.DisconnectAsync(true, CancellationToken.None);
            return string.Empty;
        }
    }
}