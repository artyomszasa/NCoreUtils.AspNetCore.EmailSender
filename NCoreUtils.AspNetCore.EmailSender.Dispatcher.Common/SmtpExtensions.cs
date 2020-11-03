using MimeKit;

namespace NCoreUtils.AspNetCore.EmailSender.Dispatcher
{
    public static class SmtpExtensions
    {
        public static MailboxAddress ToMailboxAddress(this EmailAddress source)
            => new MailboxAddress(source.Name, source.Email);
    }
}