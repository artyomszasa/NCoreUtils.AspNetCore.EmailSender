using System.Collections.Generic;

namespace NCoreUtils
{
    // FIXME: alloc free lists...
    public struct EmailMessageBuilder
    {
        public EmailAddress? From { get; set; }

        public ListWrapper<EmailAddress> To;

        public string? Subject { get; set; }

        public ListWrapper<EmailAddress> Cc;

        public ListWrapper<EmailAddress> Bcc;

        public ListWrapper<EmailContent> Contents;

        public ListWrapper<EmailAttachment> Attachments;

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