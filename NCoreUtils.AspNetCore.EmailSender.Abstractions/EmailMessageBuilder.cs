using System.Collections.Generic;

namespace NCoreUtils
{
    // FIXME: alloc free lists...
    public struct EmailMessageBuilder
    {
        public EmailAddress? From { get; set; }

        public List<EmailAddress> To { get; set; }

        public string? Subject { get; set; }

        public List<EmailAddress> Cc { get; set; }

        public List<EmailAddress> Bcc { get; set; }

        public List<EmailContent> Contents { get; set; }

        public List<EmailAttachment> Attachments { get; set; }

        public EmailMessage Build()
            => new EmailMessage(
                From!,
                To.ToArray(),
                Subject!,
                Cc.ToArray(),
                Bcc.ToArray(),
                Contents.ToArray(),
                Attachments.ToArray()
            );
    }
}