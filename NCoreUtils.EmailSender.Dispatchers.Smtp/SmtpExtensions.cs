using MimeKit;

namespace NCoreUtils.EmailSender.Dispatchers.Smtp
{
    public static class SmtpExtensions
    {
        public static MailboxAddress ToMailboxAddress(this EmailAddress source)
            => new MailboxAddress(source.Name, source.Email);
    }
}